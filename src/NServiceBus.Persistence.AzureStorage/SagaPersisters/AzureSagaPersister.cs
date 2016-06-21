namespace NServiceBus.Persistence.AzureStorage
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Extensibility;
    using Logging;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using Sagas;
    using SecondaryIndices;

    class AzureSagaPersister : ISagaPersister
    {
        public AzureSagaPersister(string connectionString, bool autoUpdateSchema)
        {
            this.autoUpdateSchema = autoUpdateSchema;
            var account = CloudStorageAccount.Parse(connectionString);
            client = account.CreateCloudTableClient();

            secondaryIndices = new SecondaryIndexPersister(GetTable, ScanForSaga, Persist);
        }

        public async Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
        {
            // The following operations have to be executed sequentially:
            // 1) insert the 2nd index, containing the primary saga data (just in case of a failure)
            // 2) insert the primary saga data in its row, storing the identifier of the secondary index as well (for completions)
            // 3) remove the data of the primary from the 2nd index. It will be no longer needed

            var secondaryIndexKey = await secondaryIndices.Insert(sagaData, correlationProperty).ConfigureAwait(false);
            await Persist(sagaData, secondaryIndexKey).ConfigureAwait(false);
            await secondaryIndices.MarkAsHavingPrimaryPersisted(sagaData, correlationProperty).ConfigureAwait(false);
        }

        public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            return Persist(sagaData, null);
        }

        public async Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            var id = sagaId.ToString();
            var entityType = typeof(TSagaData);
            var tableEntity = await GetDictionaryTableEntity(id, entityType).ConfigureAwait(false);
            var entity = DictionaryTableEntityExtensions.ToEntity<TSagaData>(tableEntity);

            if (!Equals(entity, default(TSagaData)))
            {
                etags.Add(entity, tableEntity.ETag);
                EntityProperty value;
                if (tableEntity.TryGetValue(SecondaryIndexIndicatorProperty, out value))
                {
                    secondaryIndexLocalCache.Add(entity, PartitionRowKeyTuple.Parse(value.StringValue));
                }
            }

            return entity;
        }

        public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            return GetByCorrelationProperty<TSagaData>(propertyName, propertyValue, session, context, false);
        }

        public async Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var tableName = sagaData.GetType().Name;
            var table = client.GetTableReference(tableName);

            var sagaId = sagaData.Id;
            var query = GenerateSagaTableQuery<DictionaryTableEntity>(sagaId);

            var entity = (await table.ExecuteQueryAsync(query).ConfigureAwait(false)).SafeFirstOrDefault();
            if (entity == null)
            {
                return; // should not try to delete saga data that does not exist, this situation can occur on retry or parallel execution
            }

            await table.DeleteIgnoringNotFound(entity).ConfigureAwait(false);
            try
            {
                await RemoveSecondaryIndex(sagaData).ConfigureAwait(false);
            }
            catch
            {
                log.Warn($"Removal of the secondary index entry for the following saga failed: '{sagaId}'");
            }
        }

        async Task<TSagaData> GetByCorrelationProperty<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context, bool triedAlreadyOnce)
            where TSagaData : IContainSagaData
        {
            var sagaId = await secondaryIndices.FindSagaIdAndCreateIndexEntryIfNotFound<TSagaData>(propertyName, propertyValue).ConfigureAwait(false);
            if (sagaId == null)
            {
                return default(TSagaData);
            }

            var sagaData = await Get<TSagaData>(sagaId.Value, session, context).ConfigureAwait(false);
            if (Equals(sagaData, default(TSagaData)))
            {
                // saga is not found, try invalidate cache and try getting value one more time
                secondaryIndices.InvalidateCacheIfAny(propertyName, propertyValue, typeof(TSagaData));
                if (triedAlreadyOnce == false)
                {
                    return await GetByCorrelationProperty<TSagaData>(propertyName, propertyValue, session, context, true).ConfigureAwait(false);
                }
            }

            return sagaData;
        }

        public static TableQuery<TEntity> GenerateSagaTableQuery<TEntity>(Guid sagaId)
        {
            return new TableQuery<TEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sagaId.ToString()));
        }

        Task RemoveSecondaryIndex(IContainSagaData sagaData)
        {
            PartitionRowKeyTuple secondaryIndexKey;
            if (secondaryIndexLocalCache.TryGetValue(sagaData, out secondaryIndexKey))
            {
                return secondaryIndices.RemoveSecondary(sagaData.GetType(), secondaryIndexKey);
            }

            return TaskEx.CompletedTask;
        }

        async Task<DictionaryTableEntity> GetDictionaryTableEntity(string sagaId, Type entityType)
        {
            var tableName = entityType.Name;
            var table = client.GetTableReference(tableName);

            var query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sagaId));

            try
            {
                var tableEntity = (await table.ExecuteQueryAsync(query).ConfigureAwait(false)).SafeFirstOrDefault();
                return tableEntity;
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == (int) HttpStatusCode.NotFound)
                {
                    return null;
                }

                throw;
            }
        }

        async Task Persist(IContainSagaData saga, PartitionRowKeyTuple secondaryIndexKey)
        {
            var type = saga.GetType();
            var table = await GetTable(type).ConfigureAwait(false);

            var partitionKey = saga.Id.ToString();

            var batch = new TableBatchOperation();

            AddObjectToBatch(batch, saga, partitionKey, secondaryIndexKey);

            await table.ExecuteBatchAsync(batch).ConfigureAwait(false);
        }

        async Task<CloudTable> GetTable(Type sagaType)
        {
            var tableName = sagaType.Name;
            var table = client.GetTableReference(tableName);
            if (autoUpdateSchema && !tableCreated.ContainsKey(tableName))
            {
                await table.CreateIfNotExistsAsync().ConfigureAwait(false);
                tableCreated[tableName] = true;
            }
            return table;
        }

        async Task<Guid[]> ScanForSaga(Type sagaType, string propertyName, object propertyValue)
        {
            var query = DictionaryTableEntityExtensions.BuildWherePropertyQuery(sagaType, propertyName, propertyValue);
            query.SelectColumns = new List<string>
            {
                "PartitionKey",
                "RowKey"
            };

            var tableName = sagaType.Name;
            var table = client.GetTableReference(tableName);
            var entities = await table.ExecuteQueryAsync(query).ConfigureAwait(false);
            return entities.Select(entity => Guid.ParseExact(entity.PartitionKey, "D")).ToArray();
        }

        static void AddObjectToBatch(TableBatchOperation batch, object entity, string partitionKey, PartitionRowKeyTuple secondaryIndexKey, string rowkey = "")
        {
            if (rowkey == "")
            {
                // just to be backward compat with original implementation
                rowkey = partitionKey;
            }

            var type = entity.GetType();
            string etag;

            var update = etags.TryGetValue(entity, out etag);

            if (secondaryIndexKey == null && update)
            {
                secondaryIndexLocalCache.TryGetValue(entity, out secondaryIndexKey);
            }

            var properties = SelectPropertiesToPersist(type);

            var toPersist = DictionaryTableEntityExtensions.ToDictionaryTableEntity(entity, new DictionaryTableEntity
            {
                PartitionKey = partitionKey,
                RowKey = rowkey,
                ETag = etag
            }, properties);

            if (secondaryIndexKey != null)
            {
                toPersist.Add(SecondaryIndexIndicatorProperty, secondaryIndexKey.ToString());
            }

            //no longer using InsertOrReplace as it ignores concurrency checks
            batch.Add(update ? TableOperation.Replace(toPersist) : TableOperation.Insert(toPersist));
        }

        internal static PropertyInfo[] SelectPropertiesToPersist(Type sagaType)
        {
            return sagaType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        }

        readonly ILog log = LogManager.GetLogger<AzureSagaPersister>();

        bool autoUpdateSchema;
        CloudTableClient client;
        SecondaryIndexPersister secondaryIndices;
        const string SecondaryIndexIndicatorProperty = "NServiceBus_2ndIndexKey";
        static ConcurrentDictionary<string, bool> tableCreated = new ConcurrentDictionary<string, bool>();
        static ConditionalWeakTable<object, string> etags = new ConditionalWeakTable<object, string>();
        static ConditionalWeakTable<object, PartitionRowKeyTuple> secondaryIndexLocalCache = new ConditionalWeakTable<object, PartitionRowKeyTuple>();
    }
}