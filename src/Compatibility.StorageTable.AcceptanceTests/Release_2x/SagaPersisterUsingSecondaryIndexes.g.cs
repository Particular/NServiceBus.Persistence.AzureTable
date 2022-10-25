namespace NServiceBus.Persistence.AzureTable.Release_2x
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Microsoft.Azure.Cosmos.Table;
    using System.Net;
    using System.Reflection;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTests;
    using Extensibility;
    using Logging;
    using Sagas;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// This is a copy of the saga persister code 2.4.1
    /// <remarks>Table name needs to include table prefix <code>var tableName = $"{SetupFixture.TablePrefix}{sagaType.Name}";</code>, see GetTable</remarks>
    /// </summary>
    public class SagaPersisterUsingSecondaryIndexes : ISagaPersister
    {
        public SagaPersisterUsingSecondaryIndexes(string connectionString, bool autoUpdateSchema, bool assumeSecondaryIndicesExist = false)
        {
            this.autoUpdateSchema = autoUpdateSchema;
            var account = CloudStorageAccount.Parse(connectionString);
            client = account.CreateCloudTableClient();

            secondaryIndices = new SecondaryIndexPersister(GetTable, ScanForSaga, Persist, assumeSecondaryIndicesExist);
        }

        public async Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
        {
            // The following operations have to be executed sequentially:
            // 1) insert the 2nd index, containing the primary saga data (just in case of a failure)
            // 2) insert the primary saga data in its row, storing the identifier of the secondary index as well (for completions)
            // 3) remove the data of the primary from the 2nd index. It will be no longer needed
            var secondaryIndexKey = await secondaryIndices.Insert(sagaData, correlationProperty, context).ConfigureAwait(false);
            await Persist(sagaData, secondaryIndexKey, context).ConfigureAwait(false);
            await secondaryIndices.MarkAsHavingPrimaryPersisted(sagaData, correlationProperty).ConfigureAwait(false);
        }

        public Task Update(IContainSagaData sagaData, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
        {
            return Persist(sagaData, null, context);
        }

        public async Task<TSagaData> Get<TSagaData>(Guid sagaId, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
            where TSagaData : class, IContainSagaData
        {
            var id = sagaId.ToString();
            var entityType = typeof(TSagaData);
            var tableEntity = await GetDictionaryTableEntity(id, entityType).ConfigureAwait(false);
            var entity = DictionaryTableEntityExtensions.ToEntity<TSagaData>(tableEntity);

            if (!Equals(entity, default(TSagaData)))
            {
                var meta = context.GetOrCreate<SagaInstanceMetadata>();
                meta.AddEtag(entity, tableEntity.ETag);
                if (tableEntity.TryGetValue(SecondaryIndexIndicatorProperty, out var value))
                {
                    var partitionRowKeyTuple = PartitionRowKeyTuple.Parse(value.StringValue);
                    if (partitionRowKeyTuple.HasValue)
                    {
                        meta.AddSecondaryIndexId(entity, partitionRowKeyTuple.Value);
                    }
                }
            }

            return entity;
        }

        public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
            where TSagaData : class, IContainSagaData
        {
            return GetByCorrelationProperty<TSagaData>(propertyName, propertyValue, session, context, false);
        }

        public async Task Complete(IContainSagaData sagaData, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
        {
            var table = await GetTable(sagaData.GetType()).ConfigureAwait(false);

            var sagaId = sagaData.Id.ToString();
            var meta = context.GetOrCreate<SagaInstanceMetadata>();
            if (!meta.TryGetEtag(sagaData, out var etag))
            {
                etag = "*";
            }

            var entity = new DictionaryTableEntity
            {
                ETag = etag,
                PartitionKey = sagaId,
                RowKey = sagaId
            };
            try
            {
                await table.ExecuteAsync(TableOperation.Delete(entity)).ConfigureAwait(false);
            }
            catch (StorageException e) when (e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
            {
                // should not try to delete saga data that does not exist, this situation can occur on retry or parallel execution
            }

            try
            {
                await RemoveSecondaryIndex(sagaData, meta).ConfigureAwait(false);
            }
            catch
            {
                log.Warn($"Removal of the secondary index entry for the following saga failed: '{sagaId}'");
            }
        }

        async Task<TSagaData> GetByCorrelationProperty<TSagaData>(string propertyName, object propertyValue, ISynchronizedStorageSession session, ContextBag context, bool triedAlreadyOnce)
            where TSagaData : class, IContainSagaData
        {
            var sagaId = await secondaryIndices.FindSagaIdAndCreateIndexEntryIfNotFound<TSagaData>(propertyName, propertyValue).ConfigureAwait(false);
            if (sagaId == null)
            {
                return null;
            }

            var sagaData = await Get<TSagaData>(sagaId.Value, session, context).ConfigureAwait(false);
            if (sagaData == null)
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

        public static TableQuery<TEntity> GenerateSagaTableQuery<TEntity>(Guid sagaId) where TEntity : ITableEntity, new()
        {
            var query = new TableQuery<TEntity>();
            return query.Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sagaId.ToString()));
        }

        Task RemoveSecondaryIndex(IContainSagaData sagaData, SagaInstanceMetadata meta)
        {
            if (meta.TryGetSecondaryIndexKey(sagaData, out var secondaryIndexKey))
            {
                return secondaryIndices.RemoveSecondary(sagaData.GetType(), secondaryIndexKey.Value);
            }

            return Task.CompletedTask;
        }

        async Task<DictionaryTableEntity> GetDictionaryTableEntity(string sagaId, Type entityType)
        {
            var table = await GetTable(entityType).ConfigureAwait(false);

            var query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sagaId));

            try
            {
                var tableEntity = (await table.ExecuteQueryAsync(query).ConfigureAwait(false)).SafeFirstOrDefault();
                return tableEntity;
            }
            catch (StorageException e) when (e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        async Task Persist(IContainSagaData saga, PartitionRowKeyTuple? secondaryIndexKey, ContextBag context)
        {
            var type = saga.GetType();
            var table = await GetTable(type).ConfigureAwait(false);

            var partitionKey = saga.Id.ToString();

            var batch = new TableBatchOperation();

            AddObjectToBatch(batch, saga, partitionKey, secondaryIndexKey, context);

            await table.ExecuteBatchAsync(batch).ConfigureAwait(false);
        }

        async Task<CloudTable> GetTable(Type sagaType)
        {
            var tableName = $"{SetupFixture.TablePrefix}{sagaType.Name}";
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

            var table = await GetTable(sagaType).ConfigureAwait(false);
            var entities = await table.ExecuteQueryAsync(query).ConfigureAwait(false);
            return entities.Select(entity => Guid.ParseExact(entity.PartitionKey, "D")).ToArray();
        }

        static void AddObjectToBatch(TableBatchOperation batch, object entity, string partitionKey, PartitionRowKeyTuple? secondaryIndexKey, ContextBag context)
        {
            var rowkey = partitionKey;

            var type = entity.GetType();

            var meta = context.GetOrCreate<SagaInstanceMetadata>();
            var update = meta.TryGetEtag(entity, out var etag);

            if (secondaryIndexKey == null && update)
            {
                meta.TryGetSecondaryIndexKey(entity, out secondaryIndexKey);
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

        readonly ILog log = LogManager.GetLogger<SagaPersisterUsingSecondaryIndexes>();

        bool autoUpdateSchema;
        CloudTableClient client;
        SecondaryIndexPersister secondaryIndices;
        const string SecondaryIndexIndicatorProperty = "NServiceBus_2ndIndexKey";
        static ConcurrentDictionary<string, bool> tableCreated = new ConcurrentDictionary<string, bool>();

        /// <summary>
        /// Holds saga instance related metadata in a scope of a <see cref="ContextBag" />.
        /// </summary>
        class SagaInstanceMetadata
        {
            public void AddEtag(IContainSagaData entity, string etag)
            {
                etags[entity] = etag;
            }

            public void AddSecondaryIndexId(IContainSagaData entity, PartitionRowKeyTuple secondaryIndexKey)
            {
                secondaryIndexKeys[entity] = secondaryIndexKey;
            }

            public bool TryGetEtag(object entity, out string etag)
            {
                return etags.TryGetValue(entity, out etag);
            }

            public bool TryGetSecondaryIndexKey(object entity, out PartitionRowKeyTuple? secondaryIndexKey)
            {
                return secondaryIndexKeys.TryGetValue(entity, out secondaryIndexKey);
            }

            Dictionary<object, string> etags = new Dictionary<object, string>();
            Dictionary<object, PartitionRowKeyTuple?> secondaryIndexKeys = new Dictionary<object, PartitionRowKeyTuple?>();
        }
    }
}