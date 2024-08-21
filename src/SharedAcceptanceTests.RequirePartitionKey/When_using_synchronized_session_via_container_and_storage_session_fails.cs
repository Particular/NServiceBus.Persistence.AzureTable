namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Azure;
    using Azure.Data.Tables;
    using NUnit.Framework;
    using Persistence.AzureTable;
    using ITableEntity = Azure.Data.Tables.ITableEntity;

    [TestFixture]
    public class When_using_synchronized_session_via_container_and_storage_session_fails : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_roll_back_all_operations()
        {
            // not possible to intercept cosmos API calls with OperationContext
            Requires.AzureStorageTable();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(
                    b => b.DoNotFailOnErrorMessages()
                          .When(s => s.SendLocal(new MyMessage())))
                .Done(c => c.FirstHandlerIsDone && c.FailedMessages.Any())
                .Run();

            Assert.That(context.BatchIdentifiers, Is.Empty, "Expected to have no transactional batch but found one.");
        }

        public class Context : ScenarioContext
        {
            public const string Item1_Id = nameof(Item1_Id);
            public const string Item2_Id = nameof(Item2_Id);

            public bool FirstHandlerIsDone { get; set; }
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
                        RowKey = Context.Item1_Id,
                        PartitionKey = context.TestRunId.ToString(),
                        Data = "MyCustomData"
                    };
                    session.Batch.Add(new TableTransactionAction(TableTransactionActionType.Add, entity));
                    context.FirstHandlerIsDone = true;

                    return Task.CompletedTask;
                }

                readonly Context context;
                readonly IAzureTableStorageSession session;
            }

            public class MyHandlerUsingExtensionMethod : IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message, IMessageHandlerContext handlerContext)
                {
                    var session = handlerContext.SynchronizedStorageSession.AzureTablePersistenceSession();

                    var entity = new MyTableEntity
                    {
                        RowKey = Context.Item2_Id,
                        PartitionKey = session.PartitionKey,
                        Data = "MyCustomData"
                    };
                    session.Batch.Add(new TableTransactionAction(TableTransactionActionType.Add, entity));
                    throw new SimulatedException();
                }
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
        }
    }
}