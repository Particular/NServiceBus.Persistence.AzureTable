namespace NServiceBus.Persistence.AzureTable.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Data.Tables;
    using Migration;
    using NUnit.Framework;
    using Sagas;
    using Testing;

    [TestFixture]
    public class ProvidePartitionKeyFromSagaIdTests
    {
        TableClient tableClient;
        TableServiceClient tableServiceClient;
        string tableName;

        [SetUp]
        public void SetUp()
        {
            tableServiceClient = new TableServiceClient(ConnectionStringHelper.GetEnvConfiguredConnectionStringForPersistence("StorageTable"));

            // we don't really need this table to exist but this is easier than faking away tables
            tableName = nameof(ProvidePartitionKeyFromSagaIdTests).ToLower();
            tableClient = tableServiceClient.GetTableClient(tableName);
        }

        [Test]
        public async Task Should_not_override_existing_partition_key()
        {
            var secondaryIndex = new TestableSecondaryIndex();
            var provider = new ProvidePartitionKeyFromSagaId(new Provider(), null, secondaryIndex, false, string.Empty);
            var logicalMessageContext = new TestableIncomingLogicalMessageContext();

            var tableEntityPartitionKey = new TableEntityPartitionKey("some key");
            logicalMessageContext.Extensions.Set(tableEntityPartitionKey);

            await provider.SetPartitionKey<TestSagaData>(logicalMessageContext, new SagaCorrelationProperty("SomeId", Guid.NewGuid()));

            Assert.AreEqual(tableEntityPartitionKey, logicalMessageContext.Extensions.Get<TableEntityPartitionKey>());
        }

        [Test]
        public void Should_throw_with_correlation_property_none()
        {
            var secondaryIndex = new TestableSecondaryIndex();
            var provider = new ProvidePartitionKeyFromSagaId(new Provider(), new TableClientHolderResolver(new Provider(), null), secondaryIndex, false, string.Empty);
            var logicalMessageContext = new TestableIncomingLogicalMessageContext();

            var tableHolder = new TableClientHolder(tableClient);
            logicalMessageContext.Extensions.Set(tableHolder);

            var exception = Assert.ThrowsAsync<Exception>(async () => await provider.SetPartitionKey<TestSagaData>(logicalMessageContext, SagaCorrelationProperty.None));
            StringAssert.Contains("The Azure Table saga persister doesn't support custom saga finders.", exception.Message);
        }

        [Test]
        public async Task Should_set_table_information_to_table_holder_if_not_available()
        {
            var secondaryIndex = new TestableSecondaryIndex();
            var provider = new ProvidePartitionKeyFromSagaId(new Provider(), new TableClientHolderResolver(new Provider(), null), secondaryIndex, false, string.Empty);
            var logicalMessageContext = new TestableIncomingLogicalMessageContext();

            var tableHolder = new TableClientHolder(tableClient);
            logicalMessageContext.Extensions.Set(tableHolder);

            await provider.SetPartitionKey<TestSagaData>(logicalMessageContext, new SagaCorrelationProperty("SomeId", Guid.NewGuid()));

            var tableInformation = logicalMessageContext.Extensions.Get<TableInformation>();

            Assert.AreEqual(tableClient.Name, tableInformation.TableName);
        }

        [Test]
        public async Task Should_set_table_information_by_convention_when_no_table_holder()
        {
            var secondaryIndex = new TestableSecondaryIndex();
            var tableServiceClientProvider = new Provider
            {
                Client = tableServiceClient
            };
            var provider = new ProvidePartitionKeyFromSagaId(tableServiceClientProvider, new TableClientHolderResolver(tableServiceClientProvider, null), secondaryIndex, false, string.Empty);
            var logicalMessageContext = new TestableIncomingLogicalMessageContext();

            await provider.SetPartitionKey<TestSagaData>(logicalMessageContext, new SagaCorrelationProperty("SomeId", Guid.NewGuid()));

            var tableInformation = logicalMessageContext.Extensions.Get<TableInformation>();

            Assert.AreEqual(nameof(TestSagaData), tableInformation.TableName);
        }

        [Test]
        public async Task Should_not_override_existing_table_information()
        {
            var secondaryIndex = new TestableSecondaryIndex();
            var provider = new ProvidePartitionKeyFromSagaId(new Provider(), new TableClientHolderResolver(new Provider(), null), secondaryIndex, false, string.Empty);
            var logicalMessageContext = new TestableIncomingLogicalMessageContext();

            var tableHolder = new TableClientHolder(tableClient);
            logicalMessageContext.Extensions.Set(tableHolder);

            var tableInformation = new TableInformation("MyTable");
            logicalMessageContext.Extensions.Set(tableInformation);

            await provider.SetPartitionKey<TestSagaData>(logicalMessageContext, new SagaCorrelationProperty("SomeId", Guid.NewGuid()));

            Assert.AreEqual(tableInformation, logicalMessageContext.Extensions.Get<TableInformation>());
        }

        [Test]
        public async Task Should_set_partition_key_to_sagaid_header()
        {
            var secondaryIndex = new TestableSecondaryIndex();
            var provider = new ProvidePartitionKeyFromSagaId(new Provider(), new TableClientHolderResolver(new Provider(), null), secondaryIndex, false, string.Empty);
            var logicalMessageContext = new TestableIncomingLogicalMessageContext();

            var tableHolder = new TableClientHolder(tableClient);
            logicalMessageContext.Extensions.Set(tableHolder);

            var sagaId = Guid.NewGuid().ToString();
            logicalMessageContext.Headers.Add(Headers.SagaId, sagaId);

            await provider.SetPartitionKey<TestSagaData>(logicalMessageContext, new SagaCorrelationProperty("SomeId", Guid.NewGuid()));

            Assert.AreEqual(sagaId, logicalMessageContext.Extensions.Get<TableEntityPartitionKey>().PartitionKey);
        }

        [Test]
        public async Task Should_use_found_sagaid_from_secondary_index_when_compatibility_mode_enabled()
        {
            var secondaryIndex = new TestableSecondaryIndex();
            var migrationModeEnabled = true;
            var provider = new ProvidePartitionKeyFromSagaId(new Provider(), new TableClientHolderResolver(new Provider(), null), secondaryIndex, migrationModeEnabled, string.Empty);
            var logicalMessageContext = new TestableIncomingLogicalMessageContext();

            var tableHolder = new TableClientHolder(tableClient);
            logicalMessageContext.Extensions.Set(tableHolder);

            Guid? sagaId = Guid.NewGuid();
            secondaryIndex.Result = sagaId;

            await provider.SetPartitionKey<TestSagaData>(logicalMessageContext, new SagaCorrelationProperty("SomeId", Guid.NewGuid()));

            Assert.AreEqual(sagaId.ToString(), logicalMessageContext.Extensions.Get<TableEntityPartitionKey>().PartitionKey);
        }

        [Test]
        public async Task Should_fallback_to_deterministic_id_when_compatibility_mode_enabled()
        {
            var secondaryIndex = new TestableSecondaryIndex();
            var migrationModeEnabled = true;
            var provider = new ProvidePartitionKeyFromSagaId(new Provider(), new TableClientHolderResolver(new Provider(), null), secondaryIndex, migrationModeEnabled, string.Empty);
            var logicalMessageContext = new TestableIncomingLogicalMessageContext();

            var tableHolder = new TableClientHolder(tableClient);
            logicalMessageContext.Extensions.Set(tableHolder);

            await provider.SetPartitionKey<TestSagaData>(logicalMessageContext, new SagaCorrelationProperty("SomeId", Guid.NewGuid()));

            Assert.That(Guid.TryParse(logicalMessageContext.Extensions.Get<TableEntityPartitionKey>().PartitionKey, out _), Is.True);
        }

        [Test]
        public async Task Should_fallback_to_deterministic_id_when_compatibility_mode_disabled()
        {
            var secondaryIndex = new TestableSecondaryIndex();
            var migrationModeEnabled = false;
            var provider = new ProvidePartitionKeyFromSagaId(new Provider(), new TableClientHolderResolver(new Provider(), null), secondaryIndex, migrationModeEnabled, string.Empty);
            var logicalMessageContext = new TestableIncomingLogicalMessageContext();

            var tableHolder = new TableClientHolder(tableClient);
            logicalMessageContext.Extensions.Set(tableHolder);

            await provider.SetPartitionKey<TestSagaData>(logicalMessageContext, new SagaCorrelationProperty("SomeId", Guid.NewGuid()));

            Assert.That(Guid.TryParse(logicalMessageContext.Extensions.Get<TableEntityPartitionKey>().PartitionKey, out _), Is.True);
            Assert.That(secondaryIndex.FindSagaIdCalled, Is.False);
        }

        class TestSagaData : ContainSagaData
        {
        }

        class TestableSecondaryIndex : SecondaryIndex
        {
            public Guid? Result { get; set; }

            public bool FindSagaIdCalled { get; private set; }

            public override Task<Guid?> FindSagaId<TSagaData>(TableClient table, SagaCorrelationProperty correlationProperty, CancellationToken cancellationToken = default)
            {
                FindSagaIdCalled = true;
                return Task.FromResult(Result);
            }
        }

        class Provider : IProvideTableServiceClient
        {
            public TableServiceClient Client { get; set; }
        }
    }
}