namespace NServiceBus.AcceptanceTests.Msmq
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class When_Audit_OverrideTimeToBeReceived_set_and_transactional_Msmq : NServiceBusAcceptanceTest
    {
        [Test]
        public void Endpoint_should_not_start_and_show_error()
        {
            bool testFoundValidMsmqRunDescriptor = false;
            ScenarioException scenarioException = null;

            try
            {
                Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>()
                    .Done(c => c.EndpointsStarted)
                    .Repeat(r => r.For<MsmqOnly>())
                    .Should(context =>
                    {
                        testFoundValidMsmqRunDescriptor = true;
                        Assert.IsFalse(context.EndpointsStarted);
                    })
                    .Run();
            }
            catch (AggregateException ax)
            {
                scenarioException = ax.InnerException as ScenarioException;
            }

            if (testFoundValidMsmqRunDescriptor)
            {
                Assert.IsNotNull(scenarioException);
                StringAssert.Contains("Setting a custom OverrideTimeToBeReceived for audits is not supported on transactional MSMQ.", scenarioException.InnerException.Message);
            }
        }
        
        public class Context : ScenarioContext { }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.Transactions().Enable();
                })
                .WithConfig<AuditConfig>(c => c.OverrideTimeToBeReceived = TimeSpan.FromHours(1));
            }
        }
    }
}
