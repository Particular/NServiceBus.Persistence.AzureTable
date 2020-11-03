namespace NServiceBus.Persistence.AzureStorage.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Testing;
    using NUnit.Framework;
    using Microsoft.Azure.Cosmos.Table;

    [TestFixture]
    public class TestableAzureStorageStorageSessionTests
    {
        [Test]
        public async Task CanBeUsed()
        {
            var transactionalBatch = new TableBatchOperation();

            var testableSession = new TestableAzureStorageStorageSession(new TableEntityPartitionKey("mypartitionkey"))
            {
                Batch = transactionalBatch
            };
            var handlerContext = new TestableInvokeHandlerContext
            {
                SynchronizedStorageSession = testableSession
            };

            var handler = new HandlerUsingSynchronizedStorageSessionExtension();
            await handler.Handle(new MyMessage(), handlerContext);

            Assert.IsNotEmpty(transactionalBatch);
        }

        class HandlerUsingSynchronizedStorageSessionExtension : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                var session = context.SynchronizedStorageSession.AzureStoragePersistenceSession();
                var myItem = new MyItem
                {
                    PartitionKey = session.PartitionKey,
                    RowKey = Guid.NewGuid().ToString()
                };
                session.Batch.Add(TableOperation.Insert(myItem));
                return Task.CompletedTask;
            }
        }

        class MyItem : TableEntity
        {
        }

        class MyMessage {}
    }
}