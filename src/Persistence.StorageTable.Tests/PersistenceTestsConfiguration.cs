namespace NServiceBus.PersistenceTesting
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Data.Tables;
    using Extensibility;
    using Newtonsoft.Json;
    using NServiceBus.Outbox;
    using NServiceBus.Sagas;
    using Persistence;
    using Persistence.AzureTable;
    using JsonSerializer = Newtonsoft.Json.JsonSerializer;

    public partial class PersistenceTestsConfiguration : IProvideTableServiceClient
    {
        public bool SupportsDtc => false;

        public bool SupportsOutbox => true;

        public bool SupportsFinders => false;

        public bool SupportsPessimisticConcurrency => false;

        public ISagaIdGenerator SagaIdGenerator { get; private set; }

        public ISagaPersister SagaStorage { get; private set; }

        public IOutboxStorage OutboxStorage { get; private set; }

        public TableServiceClient Client => SetupFixture.TableServiceClient;

        public Task Configure(CancellationToken cancellationToken = default)
        {
            // with this we have a partition key per run which makes things naturally isolated
            partitionKey = Guid.NewGuid().ToString();

            SagaIdGenerator = new SagaIdGenerator();
            var resolver = new TableClientHolderResolver(this, new TableInformation(SetupFixture.TableName));
            var tableCreator = new TableCreator(true);
            SagaStorage = new AzureSagaPersister(
                this,
                tableCreator,
                null,
                JsonSerializer.Create(),
                reader => new JsonTextReader(reader),
                writer => new JsonTextWriter(writer));

            OutboxStorage = new OutboxPersister(resolver, tableCreator);

            GetContextBagForSagaStorage = () =>
            {
                var contextBag = new ContextBag();
                // This populates the partition key required to participate in a shared transaction
                var setAsDispatchedHolder = new SetAsDispatchedHolder
                {
                    TableClientHolder = resolver.ResolveAndSetIfAvailable(contextBag)
                };
                contextBag.Set(setAsDispatchedHolder);
                contextBag.Set(new TableEntityPartitionKey(partitionKey));
                return contextBag;
            };

            GetContextBagForOutbox = () =>
            {
                var contextBag = new ContextBag();
                // This populates the partition key required to participate in a shared transaction
                var setAsDispatchedHolder = new SetAsDispatchedHolder
                {
                    TableClientHolder = resolver.ResolveAndSetIfAvailable(contextBag)
                };
                contextBag.Set(setAsDispatchedHolder);
                contextBag.Set(new TableEntityPartitionKey(partitionKey));
                return contextBag;
            };
            CreateStorageSession = () => new AzureStorageSynchronizedStorageSession(resolver);

            return Task.CompletedTask;
        }

        public Task Cleanup(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Func<ICompletableSynchronizedStorageSession> CreateStorageSession { get; private set; }

        string partitionKey;
    }
}