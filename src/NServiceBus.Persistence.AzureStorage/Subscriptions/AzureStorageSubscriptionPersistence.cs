namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Features;
    using Logging;
    using Microsoft.Azure.Cosmos.Table;
    using Persistence.AzureStorage;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    /// <summary></summary>
    public class AzureStorageSubscriptionPersistence : Feature
    {
        internal AzureStorageSubscriptionPersistence()
        {
            DependsOn<MessageDrivenSubscriptions>();

            Defaults(s =>
            {
                s.SetDefault(WellKnownConfigurationKeys.SubscriptionStorageTableName, AzureSubscriptionStorageDefaults.TableName);
                s.SetDefault(WellKnownConfigurationKeys.SubscriptionStorageCreateSchema , AzureSubscriptionStorageDefaults.CreateSchema);
            });
        }

        /// <summary></summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            // If a client has been registered in the container, it will added later in the configuration process and replace any client set here
            context.Settings.TryGet(out IProvideCloudTableClientForSubscriptions cloudTableClientProvider);
            context.Container.ConfigureComponent(builder => cloudTableClientProvider ?? new ThrowIfNoCloudTableClientForSubscriptionsProvider(), DependencyLifecycle.SingleInstance);

            // the subscription storage specific override takes precedence for backward compatibility
            string subscriptionTableName;
            if (context.Settings.TryGet<TableInformation>(out var info) && !context.Settings.HasExplicitValue(WellKnownConfigurationKeys.SubscriptionStorageTableName))
            {
                subscriptionTableName = info.TableName;
            }
            else
            {
                subscriptionTableName = context.Settings.Get<string>(WellKnownConfigurationKeys.SubscriptionStorageTableName);
            }

            var createIfNotExist = context.Settings.Get<bool>(WellKnownConfigurationKeys.SubscriptionStorageCreateSchema);
            var cacheFor = context.Settings.GetOrDefault<TimeSpan>(WellKnownConfigurationKeys.SubscriptionStorageCacheFor);

            if (createIfNotExist)
            {
                context.RegisterStartupTask(builder => new StartupTask(subscriptionTableName, builder.Build<IProvideCloudTableClientForSubscriptions>()));
            }

            context.Container.ConfigureComponent<ISubscriptionStorage>(builder => new AzureSubscriptionStorage(builder.Build<IProvideCloudTableClientForSubscriptions>(), subscriptionTableName, cacheFor), DependencyLifecycle.SingleInstance);
        }

        class StartupTask : FeatureStartupTask
        {
            public StartupTask(string subscriptionTableName, IProvideCloudTableClientForSubscriptions tableClientProvider)
            {
                this.subscriptionTableName = subscriptionTableName;
                client = tableClientProvider.Client;
            }

            protected override Task OnStart(IMessageSession session)
            {
                Logger.Info("Creating Subscription Table");
                var table = client.GetTableReference(subscriptionTableName);
                return table.CreateIfNotExistsAsync();
            }

            protected override Task OnStop(IMessageSession session)
            {
                return Task.CompletedTask;
            }

            string subscriptionTableName;
            private CloudTableClient client;
        }

        static ILog Logger => LogManager.GetLogger<AzureStorageSubscriptionPersistence>();
    }
}