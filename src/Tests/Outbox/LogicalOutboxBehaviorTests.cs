namespace NServiceBus.Persistence.AzureStorage.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using AzureStorage;
    using Testing;
    using Outbox;
    using NUnit.Framework;
    using NServiceBus.Testing;
    using Transport;
    using TransportOperation = Outbox.TransportOperation;

    [TestFixture]
    public class LogicalOutboxBehaviorTests
    {
        private CloudTable cloudTable;
        private CloudTableClient client;
        private string tableName;

        [SetUp]
        public async Task SetUp()
        {
            var account = CloudStorageAccount.Parse(Utilities.GetEnvConfiguredConnectionStringForPersistence());

            client = account.CreateCloudTableClient();
            tableName = nameof(LogicalOutboxBehaviorTests).ToLower();
            cloudTable = client.GetTableReference(tableName);
            await cloudTable.CreateIfNotExistsAsync();
        }

        [Test]
        public async Task Should_clear_added_pending_operations_and_restore_ones_from_outbox_record()
        {
            var messageId = Guid.NewGuid().ToString();

            var record = new OutboxRecord
            {
                PartitionKey = messageId,
                Dispatched = false,
                Id = messageId,
                Operations = new[]
                {
                    new TransportOperation("42", new Dictionary<string, string>
                    {
                        {"Destination", "somewhere"}
                    }, Array.Empty<byte>(), new Dictionary<string, string>()),
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

            testableContext.Extensions.Set<OutboxTransaction>(new AzureStorageOutboxTransaction(containerHolderHolderResolver, testableContext.Extensions));

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