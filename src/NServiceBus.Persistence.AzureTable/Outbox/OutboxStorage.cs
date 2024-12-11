namespace NServiceBus.Persistence.AzureTable
{
    using Features;
    using Microsoft.Extensions.DependencyInjection;
    using Outbox;

    class OutboxStorage : Feature
    {
        internal OutboxStorage()
        {
            Defaults(s => s.EnableFeatureByDefault<SynchronizedStorage>());

            DependsOn<Outbox>();
            DependsOn<SynchronizedStorage>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var installerSettings = context.Settings.Get<SynchronizedStorageInstallerSettings>();
            context.Services.AddSingleton(new TableCreator(installerSettings.Disabled));
            context.Services.AddSingleton<IOutboxStorage, OutboxPersister>();
            context.Services.AddTransient(provider => new LogicalOutboxBehavior(provider.GetRequiredService<TableClientHolderResolver>(), provider.GetRequiredService<TableCreator>()));

            context.Pipeline.Register(provider => provider.GetRequiredService<LogicalOutboxBehavior>(), "Behavior that mimics the outbox as part of the logical stage.");
        }
    }
}