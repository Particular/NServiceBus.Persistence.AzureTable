namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Azure;
    using Azure.Data.Tables;
    using NUnit.Framework;
    using Persistence.AzureTable;
    using ITableEntity = Azure.Data.Tables.ITableEntity;

    [TestFixture]
    public class When_using_synchronized_session_via_container_and_storage_session_extension : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_commit_all_operations_using_the_same_batch()
        {
            // not possible to intercept cosmos API calls with OperationContext
            Requires.AzureStorageTable();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When(s => s.SendLocal(new MyMessage())))
                .Done(c => c.FirstHandlerIsDone && c.SecondHandlerIsDone)
                .Run()
                .ConfigureAwait(false);

            Assert.AreEqual(1, context.BatchIdentifiers.Count, "Expected to have a single transactional batch but found more.");
        }

        public class Context : ScenarioContext
        {
            public bool FirstHandlerIsDone { get; set; }
            public bool SecondHandlerIsDone { get; set; }
            public HashSet<string> BatchIdentifiers { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                var server = new BatchCountingServer();
                EndpointSetup(server, (cfg, rd) =>
                {
                    var context = rd.ScenarioContext as Context;
                    Assert.That(context, Is.Not.Null);

                    context.BatchIdentifiers = server.TransactionalBatchCounterPolicy.BatchIdentifiers;
                });
            }

            public class MyHandlerUsingStorageSession : IHandleMessages<MyMessage>
            {
                public MyHandlerUsingStorageSession(IAzureTableStorageSession session, Context context)
                {
                    this.session = session;
                    this.context = context;
                }

                public Task Handle(MyMessage message, IMessageHandlerContext handlerContext)
                {
                    var entity = new MyTableEntity
                    {
                        RowKey = Guid.NewGuid().ToString(),
                        PartitionKey = context.TestRunId.ToString(),
                        Data = "MyCustomData"
                    };
                    session.BatchOperations.Add(new TableTransactionAction(TableTransactionActionType.Add, entity));
                    context.FirstHandlerIsDone = true;

                    return Task.CompletedTask;
                }

                readonly Context context;
                readonly IAzureTableStorageSession session;
            }

            public class MyHandlerUsingExtensionMethod : IHandleMessages<MyMessage>
            {
                public MyHandlerUsingExtensionMethod(Context context) => this.context = context;

                public Task Handle(MyMessage message, IMessageHandlerContext handlerContext)
                {
                    var session = handlerContext.SynchronizedStorageSession.AzureTablePersistenceSession();

                    var entity = new MyTableEntity
                    {
                        RowKey = Guid.NewGuid().ToString(),
                        PartitionKey = session.PartitionKey,
                        Data = "MyCustomData"
                    };
                    session.BatchOperations.Add(new TableTransactionAction(TableTransactionActionType.Add, entity));
                    context.SecondHandlerIsDone = true;

                    return Task.CompletedTask;
                }

                readonly Context context;
            }
        }

        public class MyTableEntity : ITableEntity
        {
            public string Data { get; set; }
            public string PartitionKey { get; set; }
            public string RowKey { get; set; }
            public DateTimeOffset? Timestamp { get; set; }
            public ETag ETag { get; set; }
        }

        public class MyMessage : IMessage
        {
            public string Property { get; set; }
        }
    }
}