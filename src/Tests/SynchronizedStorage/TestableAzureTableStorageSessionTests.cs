namespace NServiceBus.Persistence.AzureTable.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Data.Tables;
    using Testing;
    using NUnit.Framework;
    using ITableEntity = Azure.Data.Tables.ITableEntity;

    [TestFixture]
    public class TestableAzureTableStorageSessionTests
    {
        [Test]
        public async Task CanBeUsed()
        {
            var transactionalBatch = new List<TableTransactionAction>();

            var testableSession = new TestableAzureTableStorageSession(new TableEntityPartitionKey("mypartitionkey"))
            {
                Batch = transactionalBatch
            };
            var handlerContext = new TestableInvokeHandlerContext
            {
                SynchronizedStorageSession = testableSession
            };

            var handler = new HandlerUsingSynchronizedStorageSessionExtension();
            await handler.Handle(new MyMessage(), handlerContext);

            Assert.That(transactionalBatch, Is.Not.Empty);
        }

        class HandlerUsingSynchronizedStorageSessionExtension : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                var session = context.SynchronizedStorageSession.AzureTablePersistenceSession();
                var myItem = new MyItem
                {
                    PartitionKey = session.PartitionKey,
                    RowKey = Guid.NewGuid().ToString()
                };
                session.Batch.Add(new TableTransactionAction(TableTransactionActionType.Add, myItem));
                return Task.CompletedTask;
            }
        }

        class MyItem : ITableEntity
        {
            public string PartitionKey { get; set; }
            public string RowKey { get; set; }
            public DateTimeOffset? Timestamp { get; set; }
            public ETag ETag { get; set; }
        }

        class MyMessage { }
    }
}