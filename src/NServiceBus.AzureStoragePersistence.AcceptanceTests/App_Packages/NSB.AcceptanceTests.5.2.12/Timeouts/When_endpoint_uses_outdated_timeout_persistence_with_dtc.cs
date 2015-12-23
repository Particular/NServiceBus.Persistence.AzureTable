namespace NServiceBus.AcceptanceTests.Timeouts
{
    using System.Linq;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_endpoint_uses_outdated_timeout_persistence_with_dtc : NServiceBusAcceptanceTest
    {
        [Test]
        public void Endpoint_should_start()
        {
            var resultContexts = Scenario.Define<Context>()
                .WithEndpoint<Endpoint>()
                .Done(c => c.EndpointsStarted)
                .Repeat(r => r.For<AllDtcTransports>())
                .Should(context =>
                {
                    Assert.IsTrue(context.EndpointsStarted);
                })
                .Run();
        }

        public class Context : ScenarioContext { }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.EnableFeature<TimeoutManager>();
                    config.Transactions().EnableDistributedTransactions();
                });
            }
        }

        public class Initalizer : Feature
        {
            public Initalizer()
            {
                EnableByDefault();
            }

            protected override void Setup(FeatureConfigurationContext context)
            {
                context.Container.ConfigureComponent<OutdatedTimeoutPersister>(DependencyLifecycle.SingleInstance);
            }
        }
    }
}
