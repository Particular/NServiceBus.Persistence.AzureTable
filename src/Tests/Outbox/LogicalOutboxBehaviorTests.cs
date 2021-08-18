namespace NServiceBus.Persistence.AzureTable.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using AzureTable;
    using Microsoft.Azure.Cosmos.Table;
    using NUnit.Framework;
    using Outbox;
    using Testing;
    using Transport;
    using TransportOperation = Outbox.TransportOperation;

    [TestFixture("StorageTable")]
    [TestFixture("CosmosDB")]
    public class LogicalOutboxBehaviorTests
    {
        CloudTable cloudTable;
        CloudTableClient client;
        string tableName;
        string tableApiType;

        public LogicalOutboxBehaviorTests(string tableApiType)
        {
            this.tableApiType = tableApiType;
        }

        [SetUp]
        public Task SetUp()
        {
            var account = CloudStorageAccount.Parse(ConnectionStringHelper.GetEnvConfiguredConnectionStringForPersistence(tableApiType));

            client = account.CreateCloudTableClient();
            tableName = $"{Path.GetFileNameWithoutExtension(Path.GetTempFileName())}{DateTime.UtcNow.Ticks}{nameof(LogicalOutboxBehavior)}".ToLowerInvariant();
            cloudTable = client.GetTableReference(tableName);
            return cloudTable.CreateIfNotExistsAsync();
        }

        [TearDown]
        public Task Teardown()
        {
            return cloudTable.DeleteIfExistsAsync();
        }

        [Test]
        public async Task Should_clear_added_pending_operations_and_restore_ones_from_outbox_record()
        {
            var messageId = Guid.NewGuid().ToString();

            var dispatchProperties = new DispatchProperties
            {
                DelayDeliveryWith = new DelayedDelivery.DelayDeliveryWith(TimeSpan.FromMinutes(5))
            };
            dispatchProperties["Destination"] = "DestinationQueue";

            var record = new OutboxRecord
            {
                PartitionKey = messageId,
                Dispatched = false,
                Id = messageId,
                Operations = new[]
                {
                    new TransportOperation("42", dispatchProperties, Array.Empty<byte>(), new Dictionary<string, string>()),
                }
            };

            await cloudTable.ExecuteAsync(TableOperation.Insert(record));

            var containerHolderHolderResolver = new TableHolderResolver(new Provider()
            {
                Client = client
            },
                new TableInformation(tableName));

            var behavior = new LogicalOutboxBehavior(containerHolderHolderResolver);

            var testableContext = new TestableIncomingLogicalMessageContext
            {
                MessageId = messageId
            };

            testableContext.Extensions.Set(new TableEntityPartitionKey(messageId));
            testableContext.Extensions.Set(new SetAsDispatchedHolder());

            testableContext.Extensions.Set<IOutboxTransaction>(new AzureStorageOutboxTransaction(containerHolderHolderResolver, testableContext.Extensions));

            var pendingTransportOperations = new PendingTransportOperations();
            pendingTransportOperations.Add(new Transport.TransportOperation(new OutgoingMessage(null, null, null), null));
            testableContext.Extensions.Set(pendingTransportOperations);

            await behavior.Invoke(testableContext, c => Task.CompletedTask);

            Assert.IsTrue(pendingTransportOperations.HasOperations, "Should have exactly one operation added found on the outbox record");
            Assert.AreEqual("42", pendingTransportOperations.Operations.ElementAt(0).Message.MessageId, "Should have exactly one operation added found on the outbox record");
        }
    }

    class Provider : IProvideCloudTableClient
    {
        public CloudTableClient Client { get; set; }
    }
}