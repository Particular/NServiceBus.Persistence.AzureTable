namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.Config.ConfigurationSource;
    using ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    public class DefaultPublisher : IEndpointSetupTemplate
    {
        public Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, IConfigurationSource configSource, Action<EndpointConfiguration> configurationBuilderCustomization)
        {
            return new DefaultServer(new List<Type> { typeof(SubscriptionTracer), typeof(SubscriptionTracer.Registration) }).GetConfiguration(runDescriptor, endpointConfiguration, configSource, b =>
            {
                b.Pipeline.Register<SubscriptionTracer.Registration>();
                configurationBuilderCustomization(b);
            });
        }

        class SubscriptionTracer : Behavior<ISubscribeContext>
        {
            Registration testContext;

            public SubscriptionTracer(Registration testContext)
            {
                this.testContext = testContext;
            }

            public override async Task Invoke(ISubscribeContext context, Func<Task> next)
            {
                await next().ConfigureAwait(false);

                testContext.EventsSubscribedTo.Add(context.EventType);
            }

            public class Registration : RegisterStep
            {
                public Registration()
                    : base("SubscriptionTracking", typeof(SubscriptionTracer), "Tracks subscriptions")
                {
                }

                public List<Type> EventsSubscribedTo { get; } = new List<Type>();
            }
        }
    }
}