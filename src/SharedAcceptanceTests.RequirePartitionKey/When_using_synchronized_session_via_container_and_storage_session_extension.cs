namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using Microsoft.Azure.Cosmos.Table;
    using Persistence.AzureStorage;

    [TestFixture]
    public class When_using_synchronized_session_via_container_and_storage_session_extension : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_commit_all_operations_using_the_same_batch()
        {
            // not possible to intercept cosmos API calls with OperationContext
            Requires.AzureTables();

            TransactionalBatchCounterHandler.Reset();

            await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When(s => s.SendLocal(new MyMessage())))
                .Done(c => c.FirstHandlerIsDone && c.SecondHandlerIsDone)
                .Run()
                .ConfigureAwait(false);

            Assert.AreEqual(1, TransactionalBatchCounterHandler.TotalTransactionalBatches, "Expected to have a single transactional batch but found more.");
        }

        public class Context : ScenarioContext
        {
            public bool FirstHandlerIsDone { get; set; }
            public bool SecondHandlerIsDone { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyHandlerUsingStorageSession : IHandleMessages<MyMessage>
            {
                public MyHandlerUsingStorageSession(IAzureStorageStorageSession session, Context context)
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
                    session.Batch.Add(TableOperation.Insert(entity));
                    context.FirstHandlerIsDone = true;

                    return Task.CompletedTask;
                }

                Context context;
                IAzureStorageStorageSession session;
            }

            public class MyHandlerUsingExtensionMethod : IHandleMessages<MyMessage>
            {
                public MyHandlerUsingExtensionMethod(Context context)
                {
                    this.context = context;
                }

                public Task Handle(MyMessage message, IMessageHandlerContext handlerContext)
                {
                    var session = handlerContext.SynchronizedStorageSession.AzureStoragePersistenceSession();

                    var entity = new MyTableEntity
                    {
                        RowKey = Guid.NewGuid().ToString(),
                        PartitionKey = session.PartitionKey,
                        Data = "MyCustomData"
                    };
                    session.Batch.Add(TableOperation.Insert(entity));
                    context.SecondHandlerIsDone = true;

                    return Task.CompletedTask;
                }

                Context context;
            }
        }

        public class MyTableEntity : TableEntity
        {
            public string Data { get; set; }
        }

        public class MyMessage : IMessage
        {
            public string Property { get; set; }
        }
    }
}