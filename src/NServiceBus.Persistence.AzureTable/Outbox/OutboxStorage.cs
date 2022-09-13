namespace NServiceBus.Persistence.AzureTable
{
    using Features;

    class OutboxStorage : Feature
    {
        internal OutboxStorage()
        {
            Defaults(s =>
            {
                s.EnableFeatureByDefault<SynchronizedStorage>();
            });

            DependsOn<Outbox>();
            DependsOn<SynchronizedStorage>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<OutboxPersister>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(builder => new LogicalOutboxBehavior(builder.Build<TableHolderResolver>()), DependencyLifecycle.InstancePerCall);

            context.Pipeline.Register(builder => builder.Build<LogicalOutboxBehavior>(), "Behavior that mimics the outbox as part of the logical stage.");
        }
    }
}