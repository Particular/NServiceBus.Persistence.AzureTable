namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Features;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Transport;
    using NServiceBus.Unicast.Transport;
    using NUnit.Framework;

    [TestFixture]
    public class When_using_outbox_control_message : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_work()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>()
                .Done(c => c.ProcessedControlMessage)
                .Run()
                .ConfigureAwait(false);

            Assert.True(context.ProcessedControlMessage);
        }

        public class Context : ScenarioContext
        {
            public bool ProcessedControlMessage { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint() =>
                EndpointSetup<DefaultServer>((config, runDescriptor) =>
                {
                    config.EnableOutbox();
                    config.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
                    config.RegisterStartupTask<ControlMessageSender>();
                    config.Pipeline.Register(new ControlMessageBehavior(runDescriptor.ScenarioContext as Context), "Checks for confirmation control message");
                });

            class ControlMessageSender : FeatureStartupTask
            {
                IMessageDispatcher dispatcher;

                public ControlMessageSender(IMessageDispatcher dispatcher)
                {
                    this.dispatcher = dispatcher;
                }

                protected override async Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
                {
                    var controlMessage = ControlMessageFactory.Create(MessageIntent.Subscribe);
                    // set necessary subscription control message headers
                    controlMessage.Headers.Add(Headers.SubscriptionMessageType, typeof(object).AssemblyQualifiedName);
                    controlMessage.Headers.Add(Headers.ReplyToAddress, "TestSubscriberAddress");
                    var messageOperation = new TransportOperation(controlMessage, new UnicastAddressTag(AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(Endpoint))));
                    await dispatcher.Dispatch(new TransportOperations(messageOperation), new TransportTransaction(), cancellationToken);
                }

                protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
            }

            class ControlMessageBehavior : Behavior<IIncomingPhysicalMessageContext>
            {
                Context testContext;

                public ControlMessageBehavior(Context testContext)
                {
                    this.testContext = testContext;
                }

                public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
                {
                    await next();

                    testContext.ProcessedControlMessage = true;
                }
            }
        }
    }
}