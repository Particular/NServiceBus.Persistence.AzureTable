namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using Features;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    class SubscriptionStorage : Feature
    {
        internal SubscriptionStorage()
        {
            DependsOn<MessageDrivenSubscriptions>();

            Defaults(s =>
            {
                s.SetDefault(WellKnownConfigurationKeys.SubscriptionStorageTableName, AzureSubscriptionStorageDefaults.TableName);
                s.EnableFeatureByDefault<SubscriptionStorageInstallerFeature>();
            });
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            // If a client has been registered in the container, it will added later in the configuration process and replace any client set here
            context.Settings.TryGet(out IProvideCloudTableClientForSubscriptions cloudTableClientProvider);
            context.Container.ConfigureComponent(builder => cloudTableClientProvider ?? new ThrowIfNoCloudTableClientForSubscriptionsProvider(), DependencyLifecycle.SingleInstance);

            var subscriptionTableName = context.Settings.GetSubscriptionTableName();
            var cacheFor = context.Settings.GetOrDefault<TimeSpan>(WellKnownConfigurationKeys.SubscriptionStorageCacheFor);

            context.Settings.AddStartupDiagnosticsSection(
                "NServiceBus.Persistence.AzureTable.Subscriptions",
                new
                {
                    ConnectionMechanism = cloudTableClientProvider is CloudTableClientForSubscriptionsFromConnectionString ? "ConnectionString" : "Custom",
                    TableName = subscriptionTableName,
                    CacheFor = cacheFor,
                });
           
            context.Container.ConfigureComponent<ISubscriptionStorage>(builder => new AzureSubscriptionStorage(builder.Build<IProvideCloudTableClientForSubscriptions>(), subscriptionTableName, cacheFor), DependencyLifecycle.SingleInstance);
        }
    }
}