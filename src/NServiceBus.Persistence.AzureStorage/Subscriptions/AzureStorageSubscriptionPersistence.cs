namespace NServiceBus
{
    using System.Linq;
    using System;
    using System.Threading.Tasks;
    using Features;
    using Logging;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Extensions.DependencyInjection;
    using Persistence.AzureStorage.Config;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    /// <summary></summary>
    public class AzureStorageSubscriptionPersistence : Feature
    {

        internal AzureStorageSubscriptionPersistence()
        {
#pragma warning disable 618
            DependsOn<MessageDrivenSubscriptions>();
#pragma warning restore 618

            Defaults(s =>
            {
                s.SetDefault(WellKnownConfigurationKeys.SubscriptionStorageTableName, AzureSubscriptionStorageDefaults.TableName);
                s.SetDefault(WellKnownConfigurationKeys.SubscriptionStorageCreateSchema , AzureSubscriptionStorageDefaults.CreateSchema);
            });
        }

        /// <summary></summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            if (!context.Services.Any(x => x.ServiceType == typeof(IProvideCloudTableClientForSubscriptions)))
            {
                context.Services.AddSingleton(context.Settings.Get<IProvideCloudTableClientForSubscriptions>());
            }

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
                context.RegisterStartupTask(provider => new StartupTask(subscriptionTableName, provider.GetRequiredService<IProvideCloudTableClientForSubscriptions>()));
            }

            context.Services.AddSingleton<ISubscriptionStorage>(provider => new AzureSubscriptionStorage(provider.GetRequiredService<IProvideCloudTableClientForSubscriptions>(), subscriptionTableName, cacheFor));
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