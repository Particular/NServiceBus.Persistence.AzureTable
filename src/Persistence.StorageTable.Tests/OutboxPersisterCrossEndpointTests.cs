namespace NServiceBus.PersistenceTesting
{
    using System;
    using System.Threading.Tasks;
    using Azure.Data.Tables;
    using Extensibility;
    using NServiceBus.Outbox;
    using NServiceBus.Persistence.AzureTable;
    using NUnit.Framework;

    [TestFixture]
    public class OutboxPersisterCrossEndpointTests
    {
        TableClientHolderResolver resolver;
        TableCreator tableCreator;

        [SetUp]
        public void SetUp()
        {
            resolver = new TableClientHolderResolver(new TableServiceClientProvider(), new TableInformation(SetupFixture.TableName));
            tableCreator = new TableCreator(tableCreationDisabled: true);
        }

        [Test]
        public async Task Should_allow_different_endpoints_to_independently_store_outbox_record_for_same_message()
        {
            var partitionKey = Guid.NewGuid().ToString();
            var messageId = Guid.NewGuid().ToString();

            var endpoint1Persister = new OutboxPersister("endpoint1", resolver, tableCreator);
            var endpoint2Persister = new OutboxPersister("endpoint2", resolver, tableCreator);

            var ctx1 = CreateContextBag(partitionKey);
            var ctx2 = CreateContextBag(partitionKey);

            // Both call Get() before either has stored — simulates concurrent processing of the same event
            _ = await endpoint1Persister.Get(messageId, ctx1);
            _ = await endpoint2Persister.Get(messageId, ctx2);

            // Endpoint1 stores and commits
            using var tx1 = await endpoint1Persister.BeginTransaction(ctx1);
            await endpoint1Persister.Store(new OutboxMessage(messageId, []), tx1, ctx1);
            await tx1.Commit();

            // Endpoint2 should also be able to store and commit without conflict
            using var tx2 = await endpoint2Persister.BeginTransaction(ctx2);
            await endpoint2Persister.Store(new OutboxMessage(messageId, []), tx2, ctx2);
            await tx2.Commit();

            // Both endpoints should be able to independently retrieve their own outbox record
            var record1 = await endpoint1Persister.Get(messageId, CreateContextBag(partitionKey));
            var record2 = await endpoint2Persister.Get(messageId, CreateContextBag(partitionKey));

            Assert.Multiple(() =>
            {
                Assert.That(record1, Is.Not.Null, "Endpoint1 should find its own outbox record");
                Assert.That(record2, Is.Not.Null, "Endpoint2 should find its own outbox record");
            });
        }

        ContextBag CreateContextBag(string partitionKey)
        {
            var contextBag = new ContextBag();
            contextBag.Set(new TableEntityPartitionKey(partitionKey));
            return contextBag;
        }

        class TableServiceClientProvider : IProvideTableServiceClient
        {
            public TableServiceClient Client => SetupFixture.TableServiceClient;
        }
    }
}
