namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using Features;
    using Microsoft.Extensions.DependencyInjection;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    class SubscriptionStorage : Feature
    {
        internal SubscriptionStorage()
        {
#pragma warning disable 618
            DependsOn<MessageDrivenSubscriptions>();
#pragma warning restore 618

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
            context.Services.AddSingleton(cloudTableClientProvider ?? new ThrowIfNoCloudTableClientForSubscriptionsProvider());

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

            context.Services.AddSingleton<ISubscriptionStorage>(provider => new AzureSubscriptionStorage(provider.GetRequiredService<IProvideCloudTableClientForSubscriptions>(), subscriptionTableName, cacheFor));
        }
    }
}