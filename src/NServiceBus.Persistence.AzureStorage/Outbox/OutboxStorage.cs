namespace NServiceBus.Persistence.AzureStorage
{
    using System;
    using Features;

    class OutboxStorage : Feature
    {
        OutboxStorage()
        {
            Defaults(s =>
            {
                s.EnableFeatureByDefault<SynchronizedStorage>();
            });
            DependsOn<SynchronizedStorage>();
            DependsOn<Outbox>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            //context.Container.ConfigureComponent(builder => new OutboxPersister(builder.Build<ContainerHolderResolver>(), serializer, ttlInSeconds), DependencyLifecycle.SingleInstance);
            //context.Pipeline.Register(builder => new LogicalOutboxBehavior(builder.Build<ContainerHolderResolver>(), serializer), "Behavior that mimics the outbox as part of the logical stage.");
        }
    }
}