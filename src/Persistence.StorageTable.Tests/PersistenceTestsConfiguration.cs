namespace NServiceBus.PersistenceTesting
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Microsoft.Azure.Cosmos.Table;
    using Newtonsoft.Json;
    using NServiceBus.Outbox;
    using NServiceBus.Sagas;
    using Persistence;
    using Persistence.AzureTable;
    using JsonSerializer = Newtonsoft.Json.JsonSerializer;
    using SynchronizedStorageSession = Persistence.AzureTable.SynchronizedStorageSession;

    public partial class PersistenceTestsConfiguration : IProvideCloudTableClient
    {
        public bool SupportsDtc => false;

        public bool SupportsOutbox => true;

        public bool SupportsFinders => false;

        public bool SupportsPessimisticConcurrency => false;

        public ISagaIdGenerator SagaIdGenerator { get; private set; }

        public ISagaPersister SagaStorage { get; private set; }

        public IOutboxStorage OutboxStorage { get; private set; }

        public CloudTableClient Client => SetupFixture.TableClient;

        public Task Configure(CancellationToken cancellationToken = default)
        {
            // with this we have a partition key per run which makes things naturally isolated
            partitionKey = Guid.NewGuid().ToString();

            SagaIdGenerator = new SagaIdGenerator();
            var resolver = new TableHolderResolver(this, new TableInformation(SetupFixture.TableName));
            var secondaryIndices = new SecondaryIndex();
            SagaStorage = new AzureSagaPersister(
                this,
                true,
                false,
                secondaryIndices,
                null,
                JsonSerializer.Create(),
                reader => new JsonTextReader(reader),
                writer => new JsonTextWriter(writer));

            OutboxStorage = new OutboxPersister(resolver);

            GetContextBagForSagaStorage = () =>
            {
                var contextBag = new ContextBag();
                // This populates the partition key required to participate in a shared transaction
                var setAsDispatchedHolder = new SetAsDispatchedHolder
                {
                    TableHolder = resolver.ResolveAndSetIfAvailable(contextBag)
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
                    TableHolder = resolver.ResolveAndSetIfAvailable(contextBag)
                };
                contextBag.Set(setAsDispatchedHolder);
                contextBag.Set(new TableEntityPartitionKey(partitionKey));
                return contextBag;
            };
            CreateStorageSession = () => new SynchronizedStorageSession(resolver);

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