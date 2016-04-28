namespace NServiceBus.Persistence.AzureStorage.SecondaryIndices
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using Sagas;

    class SecondaryIndexPersister
    {
        public delegate Task<Guid[]> ScanForSagas(Type sagaType, string propertyName, object propertyValue);

        const int LRUCapacity = 1000;
        LRUCache<PartitionRowKeyTuple, Guid> cache = new LRUCache<PartitionRowKeyTuple, Guid>(LRUCapacity);
        Func<Type, Task<CloudTable>> getTableForSaga;
        Func<IContainSagaData, Task> persist;
        ScanForSagas scanner;

        public SecondaryIndexPersister(Func<Type, Task<CloudTable>> getTableForSaga, ScanForSagas scanner, Func<IContainSagaData, Task> persist)
        {
            this.getTableForSaga = getTableForSaga;
            this.scanner = scanner;
            this.persist = persist;
        }

        public async Task Insert(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty)
        {
            if (correlationProperty == SagaCorrelationProperty.None)
            {
                return;
            }

            var sagaType = sagaData.GetType();
            var table = await getTableForSaga(sagaType).ConfigureAwait(false);

            var key = SecondaryIndexKeyBuilder.BuildTableKey(sagaType, correlationProperty);

            var entity = new SecondaryIndexTableEntity
            {
                SagaId = sagaData.Id,
                InitialSagaData = SagaDataSerializer.SerializeSagaData(sagaData),
                PartitionKey = key.PartitionKey,
                RowKey = key.RowKey
            };

            // the insert plan is following:
            // 1) try insert the 2nd index row
            // 2) if it fails, another worker has done it
            // 3) ensure that the primary is stored, throwing an exception afterwards in any way

            try
            {
                await table.ExecuteAsync(TableOperation.Insert(entity)).ConfigureAwait(false);
            }
            catch (StorageException ex)
            {
                var indexRowAlreadyExists = IsConflict(ex);
                if (indexRowAlreadyExists)
                {
                    var exec = await table.ExecuteAsync(TableOperation.Retrieve<SecondaryIndexTableEntity>(key.PartitionKey, key.RowKey)).ConfigureAwait(false);
                    var indexRow = (SecondaryIndexTableEntity)exec.Result;
                    var data = indexRow?.InitialSagaData;
                    if (data != null)
                    {
                        var deserializeSagaData = SagaDataSerializer.DeserializeSagaData(sagaType, data);

                        // saga hasn't been saved under primary key. Try to store it
                        try
                        {
                            await persist(deserializeSagaData).ConfigureAwait(false);
                        }
                        catch (StorageException e)
                        {
                            if (IsConflict(e))
                            {
                                // swallow ex as another worker created the primary under this key
                            }
                        }
                    }

                    throw new RetryNeededException();
                }

                throw;
            }
        }

        public async Task<Guid?> FindPossiblyCreatingIndexEntry<TSagaData>(string propertyName, object propertyValue)
            where TSagaData : IContainSagaData
        {
            if (string.IsNullOrEmpty(propertyName) || propertyValue == null)
            {
                return null;
            }

            var sagaType = typeof(TSagaData);
            var key = SecondaryIndexKeyBuilder.BuildTableKey(sagaType, new SagaCorrelationProperty(propertyName, propertyValue));

            Guid guid;
            if (cache.TryGet(key, out guid))
            {
                return guid;
            }

            var table = await getTableForSaga(sagaType).ConfigureAwait(false);
            var exec = await table.ExecuteAsync(TableOperation.Retrieve<SecondaryIndexTableEntity>(key.PartitionKey, key.RowKey))
                .ConfigureAwait(false);
            var secondaryIndexEntry = exec.Result as SecondaryIndexTableEntity;
            if (secondaryIndexEntry != null)
            {
                cache.Put(key, secondaryIndexEntry.SagaId);
                return secondaryIndexEntry.SagaId;
            }

            var ids = await scanner(sagaType, propertyName, propertyValue)
                .ConfigureAwait(false);
            if (ids == null || ids.Length == 0)
            {
                return null;
            }

            if (ids.Length > 1)
            {
                throw new DuplicatedSagaFoundException(sagaType, propertyName, ids);
            }

            var id = ids[0];

            var entity = new SecondaryIndexTableEntity();
            key.Apply(entity);
            entity.SagaId = id;

            try
            {
                await table.ExecuteAsync(TableOperation.Insert(entity)).ConfigureAwait(false);
            }
            catch (StorageException)
            {
                throw new RetryNeededException();
            }

            cache.Put(key, id);
            return id;
        }

        static bool IsConflict(StorageException ex)
        {
            return ex.RequestInformation.HttpStatusCode == (int) HttpStatusCode.Conflict;
        }
    }
}