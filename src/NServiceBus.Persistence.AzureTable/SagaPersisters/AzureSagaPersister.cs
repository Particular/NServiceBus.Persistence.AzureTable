namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Data.Tables;
    using Extensibility;
    using Newtonsoft.Json;
    using Sagas;

    sealed class AzureSagaPersister : ISagaPersister
    {
        public AzureSagaPersister(
            IProvideTableServiceClient tableServiceClientProvider,
            TableCreator tableCreator,
            bool compatibilityMode,
            SecondaryIndex secondaryIndex,
            string conventionalTablePrefix,
            JsonSerializer jsonSerializer,
            Func<TextReader, JsonReader> readerCreator,
            Func<TextWriter, JsonWriter> writerCreator)
        {
            this.writerCreator = writerCreator;
            this.readerCreator = readerCreator;
            this.jsonSerializer = jsonSerializer;
            this.conventionalTablePrefix = conventionalTablePrefix;
            this.compatibilityMode = compatibilityMode;
            this.tableCreator = tableCreator;
            client = tableServiceClientProvider.Client;
            this.secondaryIndex = secondaryIndex;
        }

        public async Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
        {
            var storageSession = (IWorkWithSharedTransactionalBatch)session;

            var partitionKey = GetPartitionKey(context, sagaData.Id);
            var sagaDataType = sagaData.GetType();

            var sagaDataEntityToSave = new TableEntity(partitionKey.PartitionKey, sagaData.Id.ToString());
            sagaDataEntityToSave = TableEntityExtensions.ToTableEntity(sagaData, sagaDataEntityToSave, jsonSerializer, writerCreator);

            var table = await GetTableClientAndCreateTableIfNotExists(storageSession, sagaDataType, cancellationToken)
                .ConfigureAwait(false);

            var meta = context.GetOrCreate<SagaInstanceMetadata>();
            meta.Entities[sagaData.Id] = (table, sagaDataEntityToSave);

            storageSession.Add(new SagaSave(partitionKey, sagaDataEntityToSave, table));
        }

        public Task Update(IContainSagaData sagaData, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
        {
            var storageSession = (IWorkWithSharedTransactionalBatch)session;
            var partitionKey = GetPartitionKey(context, sagaData.Id);

            var meta = context.GetOrCreate<SagaInstanceMetadata>();
            var (tableClient, sagaDataEntityToUpdate) = meta.Entities[sagaData.Id];

            var sagaAsTableEntity = TableEntityExtensions.ToTableEntity(sagaData, sagaDataEntityToUpdate, jsonSerializer, writerCreator);

            storageSession.Add(new SagaUpdate(partitionKey, sagaAsTableEntity, tableClient));

            return Task.CompletedTask;
        }

        public async Task<TSagaData> Get<TSagaData>(Guid sagaId, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
            where TSagaData : class, IContainSagaData
        {
            var storageSession = (IWorkWithSharedTransactionalBatch)session;

            var tableClient = await GetTableClientAndCreateTableIfNotExists(storageSession, typeof(TSagaData), cancellationToken)
                .ConfigureAwait(false);

            // reads need to go directly
            var partitionKey = GetPartitionKey(context, sagaId);

            var readSagaDataEntity = await tableClient.GetEntityIfExistsAsync<TableEntity>(partitionKey.PartitionKey, sagaId.ToString(), null, cancellationToken)
                .ConfigureAwait(false);

            if (!readSagaDataEntity.HasValue)
            {
                return default;
            }

            var sagaData = TableEntityExtensions.ToSagaData<TSagaData>(readSagaDataEntity.Value, jsonSerializer, readerCreator);
            var meta = context.GetOrCreate<SagaInstanceMetadata>();
            var entityId = sagaData.Id;
            meta.Entities[entityId] = (tableClient, readSagaDataEntity.Value);
            return sagaData;
        }

        public async Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
            where TSagaData : class, IContainSagaData
        {
            // Derive the saga id from the property name and value
            var sagaCorrelationProperty = new SagaCorrelationProperty(propertyName, propertyValue);
            var sagaId = SagaIdGenerator.Generate<TSagaData>(sagaCorrelationProperty);
            var sagaData = await Get<TSagaData>(sagaId, session, context, cancellationToken).ConfigureAwait(false);

            if (sagaData == null && compatibilityMode)
            {
                sagaData = await GetByCorrelationProperty<TSagaData>(sagaCorrelationProperty, session, context, false, cancellationToken)
                    .ConfigureAwait(false);
            }

            return sagaData;
        }

        async Task<TSagaData> GetByCorrelationProperty<TSagaData>(SagaCorrelationProperty correlationProperty, ISynchronizedStorageSession session, ContextBag context, bool triedAlreadyOnce, CancellationToken cancellationToken)
            where TSagaData : class, IContainSagaData
        {
            var storageSession = (IWorkWithSharedTransactionalBatch)session;

            var tableToReadFrom = await GetTableClientAndCreateTableIfNotExists(storageSession, typeof(TSagaData), cancellationToken)
                .ConfigureAwait(false);

            var sagaId = await secondaryIndex.FindSagaId<TSagaData>(tableToReadFrom, correlationProperty, cancellationToken).ConfigureAwait(false);
            if (sagaId == null)
            {
                return null;
            }

            var sagaData = await Get<TSagaData>(sagaId.Value, session, context, cancellationToken).ConfigureAwait(false);
            if (sagaData != null)
            {
                return sagaData;
            }
            // saga is not found, try invalidate cache and try getting value one more time
            secondaryIndex.InvalidateCacheIfAny<TSagaData>(correlationProperty);
            if (triedAlreadyOnce == false)
            {
                return await GetByCorrelationProperty<TSagaData>(correlationProperty, session, context, true, cancellationToken).ConfigureAwait(false);
            }

            return null;
        }

        async ValueTask<TableClient> GetTableClientAndCreateTableIfNotExists(IAzureTableStorageSession storageSession, Type sagaDataType, CancellationToken cancellationToken)
        {
            TableClient tableToReadFrom;
            if (storageSession.Table == null)
            {
                // to avoid string concat when nothing to do
                var sagaDataTypeName = sagaDataType.Name;
                var sagaTableNameByConvention = string.IsNullOrEmpty(conventionalTablePrefix) ?
                    sagaDataTypeName : $"{conventionalTablePrefix}{sagaDataTypeName}";
                var sagaTableByConvention = client.GetTableClient(sagaTableNameByConvention);
                tableToReadFrom = sagaTableByConvention;
            }
            else
            {
                tableToReadFrom = storageSession.Table;
            }

            await tableCreator.CreateTableIfNotExists(tableToReadFrom, cancellationToken).ConfigureAwait(false);

            return tableToReadFrom;
        }

        public Task Complete(IContainSagaData sagaData, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
        {
            var storageSession = (IWorkWithSharedTransactionalBatch)session;
            var meta = context.GetOrCreate<SagaInstanceMetadata>();
            var sagaDataEntityToDeleteTuple = meta.Entities[sagaData.Id];
            var sagaDataEntityToDelete = sagaDataEntityToDeleteTuple.Item2;
            var partitionKey = GetPartitionKey(context, sagaData.Id);

            storageSession.Add(new SagaDelete(partitionKey, sagaDataEntityToDelete, sagaDataEntityToDeleteTuple.Item1));

            //  if it is not an old saga just go ahead
            if (!sagaDataEntityToDelete.TryGetValue(SecondaryIndexIndicatorProperty, out var secondaryIndexKey))
            {
                return Task.CompletedTask;
            }

            var partitionRowKeyTuple = PartitionRowKeyTuple.Parse(secondaryIndexKey.ToString());
            if (partitionRowKeyTuple.HasValue)
            {
                // fake partition key to make sure we get a dedicated batch for this operation
                var tableEntityPartitionKey = new TableEntityPartitionKey(Guid.NewGuid().ToString());
                storageSession.Add(new SagaRemoveSecondaryIndex(tableEntityPartitionKey, sagaData.Id, secondaryIndex, partitionRowKeyTuple.Value, sagaDataEntityToDeleteTuple.Item1));
            }
            return Task.CompletedTask;
        }

        static TableEntityPartitionKey GetPartitionKey(ContextBag context, Guid sagaDataId)
            => !context.TryGet<TableEntityPartitionKey>(out var partitionKey) ? new TableEntityPartitionKey(sagaDataId.ToString()) : partitionKey;

        readonly TableCreator tableCreator;
        readonly TableServiceClient client;
        readonly SecondaryIndex secondaryIndex;
        const string SecondaryIndexIndicatorProperty = "NServiceBus_2ndIndexKey";
        readonly bool compatibilityMode;
        readonly string conventionalTablePrefix;
        readonly JsonSerializer jsonSerializer;
        readonly Func<TextReader, JsonReader> readerCreator;
        readonly Func<TextWriter, JsonWriter> writerCreator;

        /// <summary>
        /// Holds saga instance related metadata in a scope of a <see cref="ContextBag" />.
        /// </summary>
        sealed class SagaInstanceMetadata
        {
            public Dictionary<Guid, (TableClient, TableEntity)> Entities { get; } = new();
        }
    }


}