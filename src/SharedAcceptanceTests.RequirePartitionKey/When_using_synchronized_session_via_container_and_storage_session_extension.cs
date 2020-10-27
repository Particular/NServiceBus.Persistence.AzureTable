using Microsoft.Azure.Cosmos.Table;
using NServiceBus.Persistence.AzureStorage;

namespace NServiceBus.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    [TestFixture]
    public class When_using_synchronized_session_via_container_and_storage_session_extension : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_commit_all_operations_using_the_same_batch()
        {
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
            public const string Item1_Id = nameof(Item1_Id);
            public const string Item2_Id = nameof(Item2_Id);

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
                        RowKey = Context.Item1_Id,
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
                        RowKey = Context.Item2_Id,
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