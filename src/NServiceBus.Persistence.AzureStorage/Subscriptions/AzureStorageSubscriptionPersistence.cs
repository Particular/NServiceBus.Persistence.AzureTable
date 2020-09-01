namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Features;
    using Microsoft.WindowsAzure.Storage;
    using Logging;
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
#if NETFRAMEWORK
                var defaultConnectionString = System.Configuration.ConfigurationManager.AppSettings["NServiceBus/Persistence"];
                if (string.IsNullOrEmpty(defaultConnectionString) != true)
                {
                    logger.Warn(@"Connection string should be assigned using code API: var persistence = endpointConfiguration.UsePersistence<AzureStoragePersistence, StorageType.Timeouts>();\npersistence.ConnectionString(""connectionString"");");
                }
#endif
                s.SetDefault(WellKnownConfigurationKeys.SubscriptionStorageTableName, AzureSubscriptionStorageDefaults.TableName);
                s.SetDefault(WellKnownConfigurationKeys.SubscriptionStorageCreateSchema , AzureSubscriptionStorageDefaults.CreateSchema);
            });
        }

        /// <summary></summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var subscriptionTableName = context.Settings.Get<string>(WellKnownConfigurationKeys.SubscriptionStorageTableName);
            var connectionString = context.Settings.Get<string>(WellKnownConfigurationKeys.SubscriptionStorageConnectionString);
            var createIfNotExist = context.Settings.Get<bool>(WellKnownConfigurationKeys.SubscriptionStorageCreateSchema);
            var cacheFor = context.Settings.GetOrDefault<TimeSpan>(WellKnownConfigurationKeys.SubscriptionStorageCacheFor);

            if (createIfNotExist)
            {
                var startupTask = new StartupTask(subscriptionTableName, connectionString);
                context.RegisterStartupTask(startupTask);
            }

            var subscriptionStorage = new AzureSubscriptionStorage(subscriptionTableName, connectionString, cacheFor);
            context.Container.RegisterSingleton(typeof(ISubscriptionStorage), subscriptionStorage);
        }

        class StartupTask : FeatureStartupTask
        {
            ILog log = LogManager.GetLogger<StartupTask>();
            string subscriptionTableName;
            string connectionString;

            public StartupTask(string subscriptionTableName, string connectionString)
            {
                this.subscriptionTableName = subscriptionTableName;
                this.connectionString = connectionString;
            }

            protected override Task OnStart(IMessageSession session)
            {
                log.Info("Creating Subscription Table");
                var account = CloudStorageAccount.Parse(connectionString);
                var table = account.CreateCloudTableClient().GetTableReference(subscriptionTableName);
                return table.CreateIfNotExistsAsync();
            }

            protected override Task OnStop(IMessageSession session)
            {
                return Task.FromResult(0);
            }
        }

        static ILog logger => LogManager.GetLogger<AzureStorageSubscriptionPersistence>();
    }
}