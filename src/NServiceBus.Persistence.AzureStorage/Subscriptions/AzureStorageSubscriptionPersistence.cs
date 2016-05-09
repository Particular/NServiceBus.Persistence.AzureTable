namespace NServiceBus
{
    using System.Configuration;
    using System.Threading.Tasks;
    using Features;
    using Microsoft.WindowsAzure.Storage;
    using Logging;
    using Unicast.Subscriptions;

    public class AzureStorageSubscriptionPersistence : Feature
    {
        internal AzureStorageSubscriptionPersistence()
        {
            DependsOn<MessageDrivenSubscriptions>();
            Defaults(s =>
            {
                var defaultConnectionString = ConfigurationManager.AppSettings["NServiceBus/Persistence"];
                s.SetDefault("AzureSubscriptionStorage.ConnectionString", defaultConnectionString);
                s.SetDefault("AzureSubscriptionStorage.TableName", AzureSubscriptionStorageDefaults.TableName);
                s.SetDefault("AzureSubscriptionStorage.CreateSchema", AzureSubscriptionStorageDefaults.CreateSchema);
            });
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var subscriptionTableName = context.Settings.Get<string>("AzureSubscriptionStorage.TableName");
            var connectionString = context.Settings.Get<string>("AzureSubscriptionStorage.ConnectionString");
            var createIfNotExist = context.Settings.Get<bool>("AzureSubscriptionStorage.CreateSchema");

            if (createIfNotExist)
            {
                var startupTask = new StartupTask(subscriptionTableName, connectionString);
                context.RegisterStartupTask(startupTask);
            }

            context.Container.ConfigureComponent(() => new AzureSubscriptionStorage(subscriptionTableName, connectionString), DependencyLifecycle.InstancePerCall);
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

            protected override async Task OnStart(IMessageSession session)
            {
                log.Info("Creating Subscription Table");
                var account = CloudStorageAccount.Parse(connectionString);
                var table = account.CreateCloudTableClient().GetTableReference(subscriptionTableName);
                await table.CreateIfNotExistsAsync()
                    .ConfigureAwait(false);
            }

            protected override Task OnStop(IMessageSession session)
            {
                return Task.FromResult(0);
            }
        }
    }

}