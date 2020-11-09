namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using Extensibility;
    using Microsoft.Azure.Cosmos.Table;
    using Sagas;

    class AzureSagaPersister : ISagaPersister
    {
        public AzureSagaPersister(IProvideCloudTableClient tableClientProvider, bool autoUpdateSchema, bool compatibilityMode, SecondaryIndex secondaryIndex)
        {
            this.compatibilityMode = compatibilityMode;
            this.autoUpdateSchema = autoUpdateSchema;
            client = tableClientProvider.Client;
            this.secondaryIndex = secondaryIndex;
        }

        public async Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
        {
            var storageSession = (StorageSession)session;

            var partitionKey = GetPartitionKey(context, sagaData.Id);
            var sagaDataType = sagaData.GetType();

            var sagaDataEntityToSave = DictionaryTableEntityExtensions.ToDictionaryTableEntity(sagaData, new DictionaryTableEntity
            {
                PartitionKey = partitionKey.PartitionKey,
                RowKey = sagaData.Id.ToString(),
            });

            var table = await GetTableAndCreateIfNotExists(storageSession, sagaDataType)
                .ConfigureAwait(false);

            sagaDataEntityToSave.Table = table;

            var meta = context.GetOrCreate<SagaInstanceMetadata>();
            meta.Entities[sagaData] = sagaDataEntityToSave;

            storageSession.Add(new SagaSave(partitionKey, sagaDataEntityToSave));
        }

        public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var storageSession = (StorageSession)session;
            var partitionKey = GetPartitionKey(context, sagaData.Id);

            var meta = context.GetOrCreate<SagaInstanceMetadata>();
            var sagaDataEntityToUpdate = meta.Entities[sagaData];

            var sagaAsDictionaryTableEntity = DictionaryTableEntityExtensions.ToDictionaryTableEntity(sagaData, sagaDataEntityToUpdate);

            storageSession.Add(new SagaUpdate(partitionKey, sagaAsDictionaryTableEntity));

            return Task.CompletedTask;
        }

        public async Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context)
            where TSagaData : class, IContainSagaData
        {
            var storageSession = (StorageSession)session;

            var tableToReadFrom = await GetTableAndCreateIfNotExists(storageSession, typeof(TSagaData))
                .ConfigureAwait(false);

            // reads need to go directly
            var partitionKey = GetPartitionKey(context, sagaId);

            var retrieveResult = await tableToReadFrom.ExecuteAsync(
                    TableOperation.Retrieve<DictionaryTableEntity>(partitionKey.PartitionKey, sagaId.ToString()))
                .ConfigureAwait(false);

            var readSagaDataEntity = retrieveResult.Result as DictionaryTableEntity;
            var sagaNotFound = retrieveResult.HttpStatusCode == (int)HttpStatusCode.NotFound || readSagaDataEntity == null;

            if (sagaNotFound)
            {
                return default;
            }

            readSagaDataEntity.Table = tableToReadFrom;

            var sagaData = DictionaryTableEntityExtensions.ToSagaData<TSagaData>(readSagaDataEntity);
            var meta = context.GetOrCreate<SagaInstanceMetadata>();
            meta.Entities[sagaData] = readSagaDataEntity;
            return sagaData;
        }

        public async Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context)
            where TSagaData : class, IContainSagaData
        {
            // Derive the saga id from the property name and value
            var sagaCorrelationProperty = new SagaCorrelationProperty(propertyName, propertyValue);
            var sagaId = SagaIdGenerator.Generate<TSagaData>(sagaCorrelationProperty);
            var sagaData = await Get<TSagaData>(sagaId, session, context).ConfigureAwait(false);

            if (sagaData == null && compatibilityMode)
            {
                sagaData = await GetByCorrelationProperty<TSagaData>(sagaCorrelationProperty, session, context, false)
                    .ConfigureAwait(false);
            }

            return sagaData;
        }

        async Task<TSagaData> GetByCorrelationProperty<TSagaData>(SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context, bool triedAlreadyOnce)
            where TSagaData : class, IContainSagaData
        {
            var storageSession = (StorageSession)session;

            var tableToReadFrom = await GetTableAndCreateIfNotExists(storageSession, typeof(TSagaData))
                .ConfigureAwait(false);

            var sagaId = await secondaryIndex.FindSagaId<TSagaData>(tableToReadFrom, correlationProperty).ConfigureAwait(false);
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
            secondaryIndex.InvalidateCacheIfAny<TSagaData>(correlationProperty);
            if (triedAlreadyOnce == false)
            {
                return await GetByCorrelationProperty<TSagaData>(correlationProperty, session, context, true).ConfigureAwait(false);
            }

            return null;
        }

        private async Task<CloudTable> GetTableAndCreateIfNotExists(StorageSession storageSession, Type sagaDataType)
        {
            CloudTable tableToReadFrom;
            if (storageSession.Table == null)
            {
                var sagaTableNameByConvention = sagaDataType.Name;
                var sagaTableByConvention = client.GetTableReference(sagaTableNameByConvention);
                tableToReadFrom = sagaTableByConvention;
            }
            else
            {
                tableToReadFrom = storageSession.Table;
            }

            if (!autoUpdateSchema || tableCreated.TryGetValue(tableToReadFrom.Name, out var isTableCreated) ||
                isTableCreated)
            {
                return tableToReadFrom;
            }

            await tableToReadFrom.CreateIfNotExistsAsync().ConfigureAwait(false);
            tableCreated[tableToReadFrom.Name] = true;
            return tableToReadFrom;
        }

        public Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var storageSession = (StorageSession)session;
            var meta = context.GetOrCreate<SagaInstanceMetadata>();
            var sagaDataEntityToDelete = meta.Entities[sagaData];
            var partitionKey = GetPartitionKey(context, sagaData.Id);

            storageSession.Add(new SagaDelete(partitionKey, sagaDataEntityToDelete));

            //  if it is not an old saga just go ahead
            if (!sagaDataEntityToDelete.TryGetValue(SecondaryIndexIndicatorProperty, out var secondaryIndexKey))
            {
                return Task.CompletedTask;
            }

            var partitionRowKeyTuple = PartitionRowKeyTuple.Parse(secondaryIndexKey.StringValue);
            if (partitionRowKeyTuple.HasValue)
            {
                // fake partition key to make sure we get a dedicated batch for this operation
                var tableEntityPartitionKey = new TableEntityPartitionKey(Guid.NewGuid().ToString());
                storageSession.Add(new SagaRemoveSecondaryIndex(tableEntityPartitionKey, sagaData.Id, secondaryIndex, partitionRowKeyTuple.Value, sagaDataEntityToDelete.Table));
            }
            return Task.CompletedTask;
        }

        static TableEntityPartitionKey GetPartitionKey(ContextBag context, Guid sagaDataId)
        {
            if (!context.TryGet<TableEntityPartitionKey>(out var partitionKey))
            {
                partitionKey = new TableEntityPartitionKey(sagaDataId.ToString());
            }

            return partitionKey;
        }

        bool autoUpdateSchema;
        CloudTableClient client;
        SecondaryIndex secondaryIndex;
        const string SecondaryIndexIndicatorProperty = "NServiceBus_2ndIndexKey";
        static ConcurrentDictionary<string, bool> tableCreated = new ConcurrentDictionary<string, bool>();
        private readonly bool compatibilityMode;

        /// <summary>
        /// Holds saga instance related metadata in a scope of a <see cref="ContextBag" />.
        /// </summary>
        class SagaInstanceMetadata
        {
            public Dictionary<object, DictionaryTableEntity> Entities { get; } = new Dictionary<object, DictionaryTableEntity>();
        }
    }


}