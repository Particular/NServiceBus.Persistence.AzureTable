namespace NServiceBus.Persistence.AzureStorage.SecondaryIndices
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Extensibility;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using Sagas;

    class SecondaryIndexPersister
    {
        public delegate Task<Guid[]> ScanForSagas(Type sagaType, string propertyName, object propertyValue);

        public SecondaryIndexPersister(Func<Type, Task<CloudTable>> getTableForSaga, ScanForSagas scanner, Func<IContainSagaData, PartitionRowKeyTuple?, ContextBag, Task> persist, bool assumeSecondaryIndcisExist)
        {
            this.getTableForSaga = getTableForSaga;
            this.scanner = scanner;
            this.persist = persist;
            this.assumeSecondaryIndcisExist = assumeSecondaryIndcisExist;
        }

        public async Task<PartitionRowKeyTuple?> Insert(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, ContextBag context)
        {
            if (correlationProperty == SagaCorrelationProperty.None)
            {
                return null;
            }

            var sagaType = sagaData.GetType();
            var table = await getTableForSaga(sagaType).ConfigureAwait(false);

            var key = SecondaryIndexKeyBuilder.BuildTableKey(sagaType, correlationProperty);

            var newSecondaryIndexEntity = new SecondaryIndexTableEntity
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
                await table.ExecuteAsync(TableOperation.Insert(newSecondaryIndexEntity)).ConfigureAwait(false);
                return key;
            }
            catch (StorageException ex)
            {
                var indexRowAlreadyExists = IsConflict(ex);
                if (indexRowAlreadyExists)
                {
                    var exec = await table.ExecuteAsync(TableOperation.Retrieve<SecondaryIndexTableEntity>(key.PartitionKey, key.RowKey)).ConfigureAwait(false);
                    var existingSecondaryIndexEntity = (SecondaryIndexTableEntity) exec.Result;
                    var data = existingSecondaryIndexEntity?.InitialSagaData;
                    if (data != null)
                    {
                        var deserializeSagaData = SagaDataSerializer.DeserializeSagaData(sagaType, data);

                        // saga hasn't been saved under primary key. Try to store it
                        try
                        {
                            await persist(deserializeSagaData, key, context).ConfigureAwait(false);
                            return key;
                        }
                        catch (StorageException e)
                        {
                            if (!IsConflict(e))
                            {
                                // If there is no conflict, then include the exception details so we can troubleshoot better.
                                // Otherwise we can drop through and throw a general RetryNeededException below
                                throw new RetryNeededException(e);
                            }
                        }

                        throw new RetryNeededException();
                    }

                    // ReSharper disable once RedundantIfElseBlock to make it visible for a reader
                    else
                    {
                        // data is null, this means that either the entry has been created as the secondary index after scanning the table or after storing the primary
                        var sagaId = existingSecondaryIndexEntity?.SagaId;
                        if (sagaId != null)
                        {
                            var query = AzureSagaPersister.GenerateSagaTableQuery<TableEntity>(sagaId.Value);
                            var primary = (await table.ExecuteQueryAsync(query).ConfigureAwait(false)).SafeFirstOrDefault();
                            if (primary != null)
                            {
                                // if the primary exist though, it means that a retry is required as the previous saga with the specified correlation hasn't been completed yet
                                // and the secondary index isn't a leftover from a completion
                                throw new RetryNeededException();
                            }
                        }

                        try
                        {
                            //this single call replaces a pair of calls that did a Delete followed by an Insert
                            newSecondaryIndexEntity.ETag = existingSecondaryIndexEntity?.ETag ?? "*";
                            await table.ExecuteAsync(TableOperation.InsertOrReplace(newSecondaryIndexEntity)).ConfigureAwait(false);
                            return key;
                        }
                        catch (Exception exception)
                        {
                            throw new RetryNeededException(exception);
                        }
                    }
                }
                throw;
            }
        }

        public async Task<Guid?> FindSagaIdAndCreateIndexEntryIfNotFound<TSagaData>(string propertyName, object propertyValue)
            where TSagaData : IContainSagaData
        {
            var sagaType = typeof(TSagaData);
            var key = TryBuildKey(propertyName, propertyValue, sagaType);

            if (key == null)
            {
                return null;
            }

            if (cache.TryGet(key.Value, out var guid))
            {
                return guid;
            }

            var table = await getTableForSaga(sagaType).ConfigureAwait(false);
            var exec = await table.ExecuteAsync(TableOperation.Retrieve<SecondaryIndexTableEntity>(key.Value.PartitionKey, key.Value.RowKey))
                .ConfigureAwait(false);
            var secondaryIndexEntry = exec.Result as SecondaryIndexTableEntity;
            if (secondaryIndexEntry != null)
            {
                cache.Put(key.Value, secondaryIndexEntry.SagaId);
                return secondaryIndexEntry.SagaId;
            }

            if (assumeSecondaryIndcisExist)
            {
                return null;
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

            var entity = CreateIndexingOnlyEntity(key.Value, id);

            try
            {
                await table.ExecuteAsync(TableOperation.Insert(entity)).ConfigureAwait(false);
            }
            catch (StorageException)
            {
                throw new RetryNeededException();
            }

            cache.Put(key.Value, id);
            return id;
        }

        /// <summary>
        /// Invalidates the secondary index cache if any exists for the specified property value.
        /// </summary>
        public void InvalidateCacheIfAny(string propertyName, object propertyValue, Type sagaType)
        {
            var key = TryBuildKey(propertyName, propertyValue, sagaType);
            if (key != null)
            {
                cache.Remove(key.Value);
            }
        }

        static PartitionRowKeyTuple? TryBuildKey(string propertyName, object propertyValue, Type sagaType)
        {
            if (string.IsNullOrEmpty(propertyName) || propertyValue == null)
            {
                return null;
            }
            return SecondaryIndexKeyBuilder.BuildTableKey(sagaType, new SagaCorrelationProperty(propertyName, propertyValue));
        }

        public async Task MarkAsHavingPrimaryPersisted(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty)
        {
            if (correlationProperty == SagaCorrelationProperty.None)
            {
                return;
            }

            var sagaType = sagaData.GetType();
            var table = await getTableForSaga(sagaType).ConfigureAwait(false);
            var secondaryIndexKey = SecondaryIndexKeyBuilder.BuildTableKey(sagaType, correlationProperty);

            var secondaryIndexTableEntity = CreateIndexingOnlyEntity(secondaryIndexKey, sagaData.Id);
            secondaryIndexTableEntity.ETag = "*";

            await table.ExecuteAsync(TableOperation.Replace(secondaryIndexTableEntity)).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates an indexing only entity, without payload of the primary.
        /// </summary>
        static SecondaryIndexTableEntity CreateIndexingOnlyEntity(PartitionRowKeyTuple key, Guid id)
        {
            var entity = new SecondaryIndexTableEntity();
            key.Apply(entity);
            entity.SagaId = id;
            return entity;
        }

        public async Task RemoveSecondary(Type sagaType, PartitionRowKeyTuple secondaryIndexKey)
        {
            var table = await getTableForSaga(sagaType).ConfigureAwait(false);
            var e = new TableEntity
            {
                ETag = "*"
            };

            secondaryIndexKey.Apply(e);
            cache.Remove(secondaryIndexKey);
            await table.DeleteIgnoringNotFound(e).ConfigureAwait(false);
        }

        static bool IsConflict(StorageException ex)
        {
            return ex.RequestInformation.HttpStatusCode == (int) HttpStatusCode.Conflict;
        }

        LRUCache<PartitionRowKeyTuple, Guid> cache = new LRUCache<PartitionRowKeyTuple, Guid>(LRUCapacity);
        Func<Type, Task<CloudTable>> getTableForSaga;
        Func<IContainSagaData, PartitionRowKeyTuple?, ContextBag, Task> persist;
        bool assumeSecondaryIndcisExist;
        ScanForSagas scanner;

        const int LRUCapacity = 1000;
    }
}