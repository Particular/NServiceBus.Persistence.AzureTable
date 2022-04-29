namespace NServiceBus.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Microsoft.Extensions.DependencyInjection;
    using Persistence.AzureTable;
    using NUnit.Framework;

    [TestFixture]
    public partial class When_using_outbox_synchronized_session_via_container : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_inject_synchronized_session_into_handler()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When(s => s.SendLocal(new MyMessage())))
                .Done(c => c.Done)
                .Run()
                .ConfigureAwait(false);

            Assert.True(context.RepositoryHasBatch);
            Assert.True(context.RepositoryHasTable);
            AssertPartitionPart(context);
        }

        partial void AssertPartitionPart(Context scenarioContext);

        public class Context : ScenarioContext
        {
            public bool Done { get; set; }
            public bool RepositoryHasBatch { get; set; }
            public bool RepositoryHasTable { get; set; }
            public string PartitionKey { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.EnableOutbox();
                    config.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
                    config.RegisterComponents(c =>
                    {
                        c.AddScoped<MyRepository>();
                    });
                });
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public MyMessageHandler(MyRepository repository, Context context)
                {
                    this.context = context;
                    this.repository = repository;
                }


                public Task Handle(MyMessage message, IMessageHandlerContext handlerContext)
                {
                    repository.DoSomething();
                    context.Done = true;
                    return Task.CompletedTask;
                }

                Context context;
                MyRepository repository;
            }
        }

        public class MyRepository
        {
            public MyRepository(IAzureTableStorageSession storageSession, Context context)
            {
                this.storageSession = storageSession;
                this.context = context;
            }

            public void DoSomething()
            {
                context.RepositoryHasBatch = storageSession.Batch != null;
                context.RepositoryHasTable = storageSession.Table != null;
                context.PartitionKey = storageSession.PartitionKey;
            }

            IAzureTableStorageSession storageSession;
            Context context;
        }

        public class MyMessage : IMessage
        {
            public string Property { get; set; }
        }
    }
}