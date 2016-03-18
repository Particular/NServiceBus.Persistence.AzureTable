namespace NServiceBus.SagaPersisters.Azure.SecondaryIndeces
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using Sagas;

    public class SecondaryIndexPersister
    {
        public delegate Guid? ScanForSaga(Type sagaType, string propertyName, object propertyValue);

        const int LRUCapacity = 1000;
        readonly LRUCache<PartitionRowKeyTuple, Guid> cache = new LRUCache<PartitionRowKeyTuple, Guid>(LRUCapacity);
        readonly Func<Type, CloudTable> getTableForSaga;
        readonly Func<IContainSagaData, Task> persist;
        readonly ScanForSaga scanner;

        public SecondaryIndexPersister(Func<Type, CloudTable> getTableForSaga, ScanForSaga scanner, Func<IContainSagaData, Task> persist)
        {
            this.getTableForSaga = getTableForSaga;
            this.scanner = scanner;
            this.persist = persist;
        }

        public async Task Insert(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty)
        {
            var sagaType = sagaData.GetType();
            var table = getTableForSaga(sagaType);

            var ix = IndexDefinition.Get(sagaType, correlationProperty);
            if (ix == null)
            {
                return;
            }

            var propertyValue = ix.Accessor(sagaData);
            var key = ix.BuildTableKey(propertyValue);

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
                    var indexRow = table.Execute(TableOperation.Retrieve<SecondaryIndexTableEntity>(key.PartitionKey, key.RowKey)).Result as SecondaryIndexTableEntity;
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

        public async Task<Guid?> FindPossiblyCreatingIndexEntry<TSagaData>(string propertyName, object propertyValue, SagaCorrelationProperty correlationProperty)
            where TSagaData : IContainSagaData
        {
            var sagaType = typeof(TSagaData);
            var ix = IndexDefinition.Get(sagaType, correlationProperty);
            if (ix == null)
            {
                throw new ArgumentException($"Saga '{typeof(TSagaData)}' has no correlation properties. Ensure that your saga is correlated by this property and only then, mark it with `Unique` attribute.");
            }

            ix.ValidateProperty(propertyName);

            var key = ix.BuildTableKey(propertyValue);

            Guid guid;
            if (cache.TryGet(key, out guid))
            {
                return guid;
            }

            var table = getTableForSaga(sagaType);
            var secondaryIndexEntry = (await table.ExecuteAsync(TableOperation.Retrieve<SecondaryIndexTableEntity>(key.PartitionKey, key.RowKey)).ConfigureAwait(false)).Result as SecondaryIndexTableEntity;
            if (secondaryIndexEntry != null)
            {
                cache.Put(key, secondaryIndexEntry.SagaId);
                return secondaryIndexEntry.SagaId;
            }

            var sagaId = scanner(sagaType, propertyName, propertyValue);
            if (sagaId == null)
            {
                return null;
            }

            var entity = new SecondaryIndexTableEntity();
            key.Apply(entity);
            entity.SagaId = sagaId.Value;

            try
            {
                table.Execute(TableOperation.Insert(entity));
            }
            catch (StorageException)
            {
                throw new RetryNeededException();
            }

            cache.Put(key, sagaId.Value);
            return sagaId;
        }

        private static bool IsConflict(StorageException ex)
        {
            return ex.RequestInformation.HttpStatusCode == (int) HttpStatusCode.Conflict;
        }
    }
}