﻿namespace NServiceBus.Persistence.AzureStorage
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Threading.Tasks;
    using Extensibility;
    using Logging;
    using Microsoft.Azure.Cosmos.Table;
    using Sagas;

    class AzureSagaPersister : ISagaPersister
    {
        public AzureSagaPersister(string connectionString, bool autoUpdateSchema, bool migrationModeEnabled, bool assumeSecondaryIndicesExist = false)
        {
            this.migrationModeEnabled = migrationModeEnabled;
            this.autoUpdateSchema = autoUpdateSchema;
            var account = CloudStorageAccount.Parse(connectionString);
            client = account.CreateCloudTableClient();
            isPremiumEndpoint = IsPremiumEndpoint(client);

            secondaryIndices = new SecondaryIndex(GetTable, ScanForSaga, assumeSecondaryIndicesExist);
        }

        // the SDK uses exactly this method of changing the underlying executor
        static bool IsPremiumEndpoint(CloudTableClient cloudTableClient)
        {
            var lowerInvariant = cloudTableClient.StorageUri.PrimaryUri.OriginalString.ToLowerInvariant();
            return lowerInvariant.Contains("https://localhost") && cloudTableClient.StorageUri.PrimaryUri.Port != 10002 || lowerInvariant.Contains(".table.cosmosdb.") || lowerInvariant.Contains(".table.cosmos.");
        }

        public Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
        {
            // TODO: If there is no table holder at all we probably want to use the convention of using the saga type as a table name
            var storageSession = (StorageSession)session;
            var partitionKey = GetPartitionKey(context, sagaData.Id);

            var sagaDataType = sagaData.GetType();

            var meta = context.GetOrCreate<SagaInstanceMetadata>();
            meta.TryGetEtag(sagaData, out var etag);

            var properties = SelectPropertiesToPersist(sagaDataType);

            var sagaAsDictionaryTableEntity = DictionaryTableEntityExtensions.ToDictionaryTableEntity(sagaData, new DictionaryTableEntity
            {
                PartitionKey = partitionKey.PartitionKey,
                RowKey = sagaData.Id.ToString(),
                ETag = etag,
                WillBeStoredOnPremium = isPremiumEndpoint
            }, properties);

            storageSession.Batch.Add(TableOperation.Insert(sagaAsDictionaryTableEntity));

            return Task.CompletedTask;
        }

        public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            // TODO: If there is no table holder at all we probably want to use the convention of using the saga type as a table name
            var storageSession = (StorageSession)session;
            var partitionKey = GetPartitionKey(context, sagaData.Id);

            var sagaDataType = sagaData.GetType();

            var meta = context.GetOrCreate<SagaInstanceMetadata>();
            meta.TryGetEtag(sagaData, out var etag);

            var properties = SelectPropertiesToPersist(sagaDataType);

            var sagaAsDictionaryTableEntity = DictionaryTableEntityExtensions.ToDictionaryTableEntity(sagaData, new DictionaryTableEntity
            {
                PartitionKey = partitionKey.PartitionKey,
                RowKey = sagaData.Id.ToString(),
                ETag = etag,
                WillBeStoredOnPremium = isPremiumEndpoint
            }, properties);

            // regardless whether the migration mode is enabled or not make sure we never lose the property if it was there
            if (meta.TryGetSecondaryIndexKey(sagaData, out var secondaryIndexKey))
            {
                sagaAsDictionaryTableEntity[SecondaryIndexIndicatorProperty] = EntityProperty.GeneratePropertyForString(secondaryIndexKey.ToString());
            }

            storageSession.Batch.Add(TableOperation.Replace(sagaAsDictionaryTableEntity));

            return Task.CompletedTask;
        }

        public async Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context)
            where TSagaData : class, IContainSagaData
        {
            var storageSession = (StorageSession)session;

            // TODO: If there is no table holder at all we probably want to use the convention of using the saga type as a table name
            // reads need to go directly
            var table = storageSession.TableHolder.Table;
            var partitionKey = GetPartitionKey(context, sagaId);

            var retrieveResult = await table.ExecuteAsync(
                    TableOperation.Retrieve<DictionaryTableEntity>(partitionKey.PartitionKey, sagaId.ToString()))
                .ConfigureAwait(false);

            var sagaDataAsTableEntity = retrieveResult.Result as DictionaryTableEntity;
            var sagaNotFound = retrieveResult.HttpStatusCode == (int)HttpStatusCode.NotFound || sagaDataAsTableEntity == null;

            if (sagaNotFound)
            {
                return default;
            }

            sagaDataAsTableEntity.WillBeStoredOnPremium = isPremiumEndpoint;
            var sagaData = DictionaryTableEntityExtensions.ToEntity<TSagaData>(sagaDataAsTableEntity);

            // TODO: Maybe there is a smarter way to handle the etags consistently
            var meta = context.GetOrCreate<SagaInstanceMetadata>();
            meta.AddEtag(sagaData, sagaDataAsTableEntity.ETag);
            if (sagaDataAsTableEntity.TryGetValue(SecondaryIndexIndicatorProperty, out var value))
            {
                var partitionRowKeyTuple = PartitionRowKeyTuple.Parse(value.StringValue);
                if (partitionRowKeyTuple.HasValue)
                {
                    meta.AddSecondaryIndexId(sagaData, partitionRowKeyTuple.Value);
                }
            }
            return sagaData;
        }

        public async Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context)
            where TSagaData : class, IContainSagaData
        {
            // Derive the saga id from the property name and value
            var sagaId = SagaIdGenerator.Generate(typeof(TSagaData), propertyName, propertyValue);
            var sagaData = await Get<TSagaData>(sagaId, session, context).ConfigureAwait(false);

            if (sagaData == null && migrationModeEnabled)
            {
                sagaData = await GetByCorrelationProperty<TSagaData>(propertyName, propertyValue, session, context, false)
                    .ConfigureAwait(false);
            }

            return sagaData;
        }

        public async Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var storageSession = (StorageSession)session;

            // TODO: If there is no table holder at all we probably want to use the convention of using the saga type as a table name
            var table = storageSession.TableHolder.Table;
            var partitionKey = GetPartitionKey(context, sagaData.Id);

            var meta = context.GetOrCreate<SagaInstanceMetadata>();
            if (!meta.TryGetEtag(sagaData, out var etag))
            {
                etag = "*";
            }

            var sagaId = sagaData.Id.ToString();
            var entity = new DictionaryTableEntity
            {
                ETag = etag,
                PartitionKey = partitionKey.PartitionKey,
                RowKey = sagaId,
                WillBeStoredOnPremium = isPremiumEndpoint
            };
            try
            {
                await table.ExecuteAsync(TableOperation.Delete(entity)).ConfigureAwait(false);
            }
            catch (StorageException e) when (e.RequestInformation.HttpStatusCode == (int) HttpStatusCode.NotFound)
            {
                // should not try to delete saga data that does not exist, this situation can occur on retry or parallel execution
            }

            try
            {
                // regardless whether the migration mode is enabled or not make sure if there was a secondary index
                // property set try to delete the index row as best effort
                await RemoveSecondaryIndex(sagaData, meta).ConfigureAwait(false);
            }
            catch
            {
                log.Warn($"Removal of the secondary index entry for the following saga failed: '{sagaId}'");
            }
        }

        async Task<TSagaData> GetByCorrelationProperty<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context, bool triedAlreadyOnce)
            where TSagaData : class, IContainSagaData
        {
            var sagaId = await secondaryIndices.FindSagaId<TSagaData>(propertyName, propertyValue).ConfigureAwait(false);
            if (sagaId == null)
            {
                return null;
            }

            var sagaData = await Get<TSagaData>(sagaId.Value, session, context).ConfigureAwait(false);
            if (sagaData != null)
            {
                return sagaData;
            }
            // saga is not found, try invalidate cache and try getting value one more time
            secondaryIndices.InvalidateCacheIfAny(propertyName, propertyValue, typeof(TSagaData));
            if (triedAlreadyOnce == false)
            {
                return await GetByCorrelationProperty<TSagaData>(propertyName, propertyValue, session, context, true).ConfigureAwait(false);
            }

            return null;
        }

        Task RemoveSecondaryIndex(IContainSagaData sagaData, SagaInstanceMetadata meta)
        {
            if (meta.TryGetSecondaryIndexKey(sagaData, out var secondaryIndexKey))
            {
                return secondaryIndices.RemoveSecondary(sagaData.GetType(), secondaryIndexKey.Value);
            }

            return Task.CompletedTask;
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

            var table = await GetTable(sagaType).ConfigureAwait(false);
            var entities = await table.ExecuteQueryAsync(query).ConfigureAwait(false);
            return entities.Select(entity => Guid.ParseExact(entity.PartitionKey, "D")).ToArray();
        }

        internal static PropertyInfo[] SelectPropertiesToPersist(Type sagaType)
        {
            return sagaType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        }

        static TableEntityPartitionKey GetPartitionKey(ContextBag context, Guid sagaDataId)
        {
            if (!context.TryGet<TableEntityPartitionKey>(out var partitionKey))
            {
                partitionKey = new TableEntityPartitionKey(sagaDataId.ToString());
            }

            return partitionKey;
        }

        readonly ILog log = LogManager.GetLogger<AzureSagaPersister>();

        bool autoUpdateSchema;
        CloudTableClient client;
        SecondaryIndex secondaryIndices;
        const string SecondaryIndexIndicatorProperty = "NServiceBus_2ndIndexKey";
        static ConcurrentDictionary<string, bool> tableCreated = new ConcurrentDictionary<string, bool>();
        private bool isPremiumEndpoint;
        private readonly bool migrationModeEnabled;

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