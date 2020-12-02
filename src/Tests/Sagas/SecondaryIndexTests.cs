namespace NServiceBus.Persistence.AzureTable.Tests
{
    using NUnit.Framework;
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Logging;
    using Microsoft.Azure.Cosmos.Table;
    using Sagas;
    using Testing;

    [TestFixture("CosmosDB")]
    public class SecondaryIndexTests
    {
        public SecondaryIndexTests(string tableApiType)
        {
            this.tableApiType = tableApiType;
        }

        [SetUp]
        public Task SetUp()
        {
            logStatements = new StringBuilder();

            var account = CloudStorageAccount.Parse(ConnectionStringHelper.GetEnvConfiguredConnectionStringForPersistence(tableApiType));

            client = account.CreateCloudTableClient();

            tableName = $"{Path.GetFileNameWithoutExtension(Path.GetTempFileName())}{DateTime.UtcNow.Ticks}{nameof(SecondaryIndexTests)}".ToLowerInvariant();
            cloudTable = client.GetTableReference(tableName);
            return cloudTable.CreateIfNotExistsAsync();
        }

        [Test]
        public async Task When_empty_row_key_should_log_warn_and_assume_row_key_partition_key()
        {
            var someId = new Guid("E57CF37C-1CBC-4B08-8C19-3FCE2FFC0451");

            scope = LogManager.Use<TestingLoggerFactory>()
                .BeginScope(new StringWriter(logStatements));

            var secondaryIndex = new SecondaryIndex();

            var result = await secondaryIndex.FindSagaId<TestSagaData>(cloudTable,
                new SagaCorrelationProperty("SomeId", someId));

            Assert.IsNull(result);
            StringAssert.Contains("Trying to retrieve the secondary index entry with PartitionKey = 'Index_NServiceBus.Persistence.AzureTable.Tests.SecondaryIndexTests+TestSagaData_SomeId_\"e57cf37c-1cbc-4b08-8c19-3fce2ffc0451\"' and RowKey = 'string.Empty' failed", logStatements.ToString());
        }

        [Test]
        public async Task When_opt_in_for_non_empty_row_key_should_not_log_warn()
        {
            var someId = new Guid("E57CF37C-1CBC-4B08-8C19-3FCE2FFC0451");

            scope = LogManager.Use<TestingLoggerFactory>()
                .BeginScope(new StringWriter(logStatements));

            var secondaryIndex = new SecondaryIndex(assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey: true);

            var result = await secondaryIndex.FindSagaId<TestSagaData>(cloudTable,
                new SagaCorrelationProperty("SomeId", someId));

            Assert.IsNull(result);
            Assert.IsEmpty(logStatements.ToString());
        }

        [TearDown]
        public Task Teardown()
        {
            return cloudTable.DeleteIfExistsAsync();
        }

        class TestSagaData : ContainSagaData
        {
            public Guid SomeId { get; set; }
        }

        StringBuilder logStatements;
        IDisposable scope;
        private CloudTable cloudTable;
        private CloudTableClient client;
        private string tableName;
        private string tableApiType;
    }
}