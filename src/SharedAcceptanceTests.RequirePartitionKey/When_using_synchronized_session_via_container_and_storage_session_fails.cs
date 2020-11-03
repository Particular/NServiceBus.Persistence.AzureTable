using Microsoft.Azure.Cosmos.Table;

namespace NServiceBus.AcceptanceTests
{
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using Persistence.AzureStorage;

    [TestFixture]
    public class When_using_synchronized_session_via_container_and_storage_session_fails : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_roll_back_all_operations()
        {
            TransactionalBatchCounterHandler.Reset();

            await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b =>
                {
                    b.DoNotFailOnErrorMessages();
                    b.When(s => s.SendLocal(new MyMessage()));
                })
                .Done(c => c.FirstHandlerIsDone && c.FailedMessages.Any())
                .Run()
                .ConfigureAwait(false);

            Assert.AreEqual(0, TransactionalBatchCounterHandler.TotalTransactionalBatches, "Expected to have no transactional batch but found one.");
        }

        public class Context : ScenarioContext
        {
            public const string Item1_Id = nameof(Item1_Id);
            public const string Item2_Id = nameof(Item2_Id);

            public bool FirstHandlerIsDone { get; set; }
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
                    throw new SimulatedException();
                }
            }
        }

        public class MyTableEntity : TableEntity
        {
            public string Data { get; set; }
        }

        public class MyMessage : IMessage
        {
        }
    }
}