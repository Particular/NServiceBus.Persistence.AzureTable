namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.AcceptanceTesting.Support;
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
            var runSettings = new RunSettings();
            runSettings.DoNotRegisterDefaultPartitionKeyProvider();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>()
                .Done(c => c.ProcessedControlMessage)
                .Run(runSettings);

            Assert.That(context.ProcessedControlMessage, Is.True);
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
                    config.Pipeline.Register(new ControlMessageBehavior(runDescriptor.ScenarioContext as Context), "Checks that the control message was processed successfully");
                });

            class ControlMessageSender(IMessageDispatcher dispatcher) : FeatureStartupTask
            {
                protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
                {
                    var controlMessage = ControlMessageFactory.Create(MessageIntent.Subscribe);
                    var messageOperation = new TransportOperation(controlMessage, new UnicastAddressTag(AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(Endpoint))));

                    return dispatcher.Dispatch(new TransportOperations(messageOperation), new TransportTransaction(), cancellationToken);
                }

                protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
            }

            class ControlMessageBehavior(Context testContext) : Behavior<IIncomingPhysicalMessageContext>
            {
                public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
                {
                    await next();

                    testContext.ProcessedControlMessage = true;
                }
            }
        }
    }
}