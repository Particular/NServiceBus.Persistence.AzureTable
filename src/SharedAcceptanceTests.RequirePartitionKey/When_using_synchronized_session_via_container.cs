namespace NServiceBus.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using Persistence.AzureTable;

    [TestFixture]
    public class When_using_synchronized_session_via_container : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_inject_synchronized_session_into_handler()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When(s => s.SendLocal(new MyMessage())))
                .Done(c => c.Done)
                .Run();

            Assert.That(context.HandlerHasBatch, Is.True);
        }

        public class Context : ScenarioContext
        {
            public bool Done { get; set; }
            public bool HandlerHasBatch { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint() => EndpointSetup<DefaultServer>();

            public class MyHandler : IHandleMessages<MyMessage>
            {
                public MyHandler(IAzureTableStorageSession session, Context context)
                {
                    this.session = session;
                    this.context = context;
                }

                public Task Handle(MyMessage message, IMessageHandlerContext handlerContext)
                {
                    context.Done = true;
                    context.HandlerHasBatch = session.Batch != null;

                    return Task.CompletedTask;
                }

                readonly Context context;
                readonly IAzureTableStorageSession session;
            }
        }

        public class MyMessage : IMessage
        {
            public string Property { get; set; }
        }
    }
}