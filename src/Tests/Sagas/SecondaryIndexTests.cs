namespace NServiceBus.Persistence.AzureTable.Tests
{
    using NUnit.Framework;
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Core;
    using Azure.Core.Pipeline;
    using Azure.Data.Tables;
    using Logging;
    using Sagas;
    using Testing;

    [TestFixture("StorageTable")]
    [TestFixture("CosmosDB")]
    public class SecondaryIndexTests
    {
        public SecondaryIndexTests(string tableApiType) => this.tableApiType = tableApiType;

        [SetUp]
        public Task SetUp()
        {
            logStatements = new StringBuilder();
            recorder = new AzureRequestRecorder();

            string connectionString = ConnectionStringHelper.GetEnvConfiguredConnectionStringForPersistence(tableApiType);
            var options = new TableClientOptions();
            options.AddPolicy(recorder, HttpPipelinePosition.PerCall);
            tableServiceClient = new TableServiceClient(connectionString, options);

            tableName = $"{Path.GetFileNameWithoutExtension(Path.GetTempFileName())}{DateTime.UtcNow.Ticks}{nameof(SecondaryIndexTests)}".ToLowerInvariant();
            tableClient = tableServiceClient.GetTableClient(tableName);
            return tableClient.CreateIfNotExistsAsync();
        }

        [Test]
        public async Task Should_deal_with_empty_row_key()
        {
            var someId = new Guid("E57CF37C-1CBC-4B08-8C19-3FCE2FFC0451");

            scope = LogManager.Use<TestingLoggerFactory>()
                .BeginScope(new StringWriter(logStatements));

            var secondaryIndex = new SecondaryIndex(assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey: false);

            var result = await secondaryIndex.FindSagaId<TestSagaData>(tableClient,
                new SagaCorrelationProperty("SomeId", someId));

            Assert.IsNull(result);
            if (tableApiType == "CosmosDB")
            {
                StringAssert.Contains("Trying to retrieve the secondary index entry with PartitionKey = 'Index_NServiceBus.Persistence.AzureTable.Tests.SecondaryIndexTests+TestSagaData_SomeId_\"e57cf37c-1cbc-4b08-8c19-3fce2ffc0451\"' and RowKey = 'string.Empty' failed", logStatements.ToString());
            }
            else
            {
                Assert.IsEmpty(logStatements.ToString());
            }
        }

        [Test]
        public async Task Should_allow_opt_in_for_non_empty_row_key()
        {
            var someId = new Guid("E57CF37C-1CBC-4B08-8C19-3FCE2FFC0451");

            scope = LogManager.Use<TestingLoggerFactory>()
                .BeginScope(new StringWriter(logStatements));

            var secondaryIndex = new SecondaryIndex(assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey: true);

            var result = await secondaryIndex.FindSagaId<TestSagaData>(tableClient,
                new SagaCorrelationProperty("SomeId", someId));

            Assert.IsNull(result);
            Assert.IsEmpty(logStatements.ToString());
        }

        [Test]
        public async Task Should_cache_saga_id()
        {
            var someId = Guid.NewGuid();
            var sagaId = Guid.NewGuid();
            var correlationProperty = new SagaCorrelationProperty("SomeId", someId);
            PartitionRowKeyTuple partitionRowKeyTuple = SecondaryIndexKeyBuilder.BuildTableKey<TestSagaData>(correlationProperty);

            var entity = new TableEntity
            {
                PartitionKey = partitionRowKeyTuple.PartitionKey,
                RowKey = partitionRowKeyTuple.PartitionKey,
                ["SomeId"] = someId,
                ["SagaId"] = sagaId
            };
            await tableClient.AddEntityAsync(entity);

            _ = recorder.Clear();

            var secondaryIndex = new SecondaryIndex(assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey: true);

            var nonCachedResult = await secondaryIndex.FindSagaId<TestSagaData>(tableClient,
                correlationProperty);

            recorder.Print(TestContext.Out);

            var requestsBeforeCaching = recorder.Clear();

            var cachedResult = await secondaryIndex.FindSagaId<TestSagaData>(tableClient,
                correlationProperty);

            recorder.Print(TestContext.Out);

            var requestAfterCaching = recorder.Requests;

            Assert.That(nonCachedResult, Is.Not.Null);
            Assert.That(nonCachedResult, Is.EqualTo(cachedResult));
            Assert.That(requestsBeforeCaching, Is.Not.Empty);
            Assert.That(requestAfterCaching, Is.Empty);
        }

        [Test]
        public async Task Should_fall_back_to_table_scan()
        {
            var someId = Guid.NewGuid();
            var sagaId = Guid.NewGuid();
            var correlationProperty = new SagaCorrelationProperty("SomeId", someId);

            // Random partition and row key will enforce table scan
            var entity = new TableEntity
            {
                PartitionKey = Guid.NewGuid().ToString(),
                RowKey = Guid.NewGuid().ToString(),
                ["SomeId"] = someId,
                ["SagaId"] = sagaId
            };
            await tableClient.AddEntityAsync(entity);

            _ = recorder.Clear();

            var secondaryIndex = new SecondaryIndex(assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey: false);

            var tableScanResult = await secondaryIndex.FindSagaId<TestSagaData>(tableClient,
                correlationProperty);

            recorder.Print(TestContext.Out);

            var gets = recorder.Requests.Where(r => r.ToLower().Contains("get"));
            var getWithFilter = gets.Where(get =>
                    get.Contains($"$select=PartitionKey%2CRowKey&$filter=SomeId%20eq%20guid%27{someId}%27"))
                .ToArray();

            Assert.That(tableScanResult, Is.Not.Null);
            Assert.That(getWithFilter, Is.Not.Empty);
        }

        [Test]
        public async Task Should_not_fall_back_to_table_scan_when_opted_out()
        {
            var someId = Guid.NewGuid();
            var sagaId = Guid.NewGuid();
            var correlationProperty = new SagaCorrelationProperty("SomeId", someId);

            // Random partition and row key will enforce table scan
            var entity = new TableEntity
            {
                PartitionKey = Guid.NewGuid().ToString(),
                RowKey = Guid.NewGuid().ToString(),
                ["SomeId"] = someId,
                ["SagaId"] = sagaId
            };
            await tableClient.AddEntityAsync(entity);

            _ = recorder.Clear();

            var secondaryIndex = new SecondaryIndex(assumeSecondaryIndicesExist: true, assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey: true);

            var tableScanResult = await secondaryIndex.FindSagaId<TestSagaData>(tableClient,
                correlationProperty);

            recorder.Print(TestContext.Out);

            var gets = recorder.Requests.Where(r => r.ToLower().Contains("get"));
            var getWithFilter = gets.Where(get =>
                    get.Contains($"$select=PartitionKey%2CRowKey&$filter=SomeId%20eq%20guid%27{someId}%27"))
                .ToArray();

            Assert.That(tableScanResult, Is.Null);
            Assert.That(getWithFilter, Is.Empty);
        }

        [Test]
        public async Task Should_cache_saga_id_even_after_table_scan()
        {
            var someId = Guid.NewGuid();
            var sagaId = Guid.NewGuid();
            var correlationProperty = new SagaCorrelationProperty("SomeId", someId);

            // Random partition and row key will enforce table scan
            var entity = new TableEntity
            {
                PartitionKey = Guid.NewGuid().ToString(),
                RowKey = Guid.NewGuid().ToString(),
                ["SomeId"] = someId,
                ["SagaId"] = sagaId
            };
            await tableClient.AddEntityAsync(entity);

            _ = recorder.Clear();

            var secondaryIndex = new SecondaryIndex(assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey: false);

            var nonCachedResult = await secondaryIndex.FindSagaId<TestSagaData>(tableClient,
                correlationProperty);

            recorder.Print(TestContext.Out);

            var requestsBeforeCaching = recorder.Clear();

            var cachedResult = await secondaryIndex.FindSagaId<TestSagaData>(tableClient,
                correlationProperty);

            recorder.Print(TestContext.Out);

            var requestAfterCaching = recorder.Requests;

            Assert.That(nonCachedResult, Is.Not.Null);
            Assert.That(nonCachedResult, Is.EqualTo(cachedResult));
            Assert.That(requestsBeforeCaching, Is.Not.Empty);
            Assert.That(requestAfterCaching, Is.Empty);
        }

        [TearDown]
        public async Task Teardown()
        {
            scope?.Dispose();
            try
            {
                await tableServiceClient.DeleteTableAsync(tableName);
            }
            catch (RequestFailedException e) when (e.Status == (int)HttpStatusCode.NotFound)
            {
            }
        }

        class TestSagaData : ContainSagaData
        {
            public Guid SomeId { get; set; }
        }

        sealed class AzureRequestRecorder : HttpPipelinePolicy
        {
            public ConcurrentQueue<string> Requests { get; private set; } = new();

            public override void Process(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
            {
                CaptureRequest(message);
                ProcessNext(message, pipeline);
            }

            public override async ValueTask ProcessAsync(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
            {
                CaptureRequest(message);
                await ProcessNextAsync(message, pipeline);
            }

            void CaptureRequest(HttpMessage message)
                => Requests.Enqueue($"{message.Request.Method,-7} {message.Request.Uri.PathAndQuery}");

            public ConcurrentQueue<string> Clear()
            {
                var request = new ConcurrentQueue<string>(Requests);
                Requests = new ConcurrentQueue<string>();
                return request;
            }

            public void Print(TextWriter @out)
            {
                @out.WriteLine("Recorded calls to Azure Storage Services");

                foreach (var request in Requests)
                {
                    @out.WriteLine($"- {request}");
                }

                @out.WriteLine();
            }
        }

        AzureRequestRecorder recorder;
        StringBuilder logStatements;
        IDisposable scope;
        TableClient tableClient;
        TableServiceClient tableServiceClient;
        string tableName;
        string tableApiType;
    }
}