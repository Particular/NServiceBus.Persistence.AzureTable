namespace NServiceBus
{
    using System.Threading.Tasks;
    using Persistence.AzureStorage;
    using Config;
    using Features;
    using Logging;
    using Microsoft.WindowsAzure.Storage;

    public class AzureStorageTimeoutPersistence : Feature
    {
        internal AzureStorageTimeoutPersistence()
        {
            DependsOn<TimeoutManager>();
            Defaults(s =>
            {
                var config = s.GetConfigSection<AzureTimeoutPersisterConfig>() ?? new AzureTimeoutPersisterConfig();
                s.SetDefault("AzureTimeoutStorage.ConnectionString", config.ConnectionString);
                s.SetDefault("AzureTimeoutStorage.CreateSchema", config.CreateSchema);
                s.SetDefault("AzureTimeoutStorage.TimeoutManagerDataTableName", config.TimeoutManagerDataTableName);
                s.SetDefault("AzureTimeoutStorage.TimeoutDataTableName", config.TimeoutDataTableName);
                s.SetDefault("AzureTimeoutStorage.CatchUpInterval", config.CatchUpInterval);
                s.SetDefault("AzureTimeoutStorage.PartitionKeyScope", config.PartitionKeyScope);
                s.SetDefault("AzureTimeoutStorage.TimeoutStateContainerName", config.TimeoutStateContainerName);
            });
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var createIfNotExist = context.Settings.Get<bool>("AzureTimeoutStorage.CreateSchema");
            var timeoutDataTableName = context.Settings.Get<string>("AzureTimeoutStorage.TimeoutDataTableName");
            var timeoutManagerDataTableName = context.Settings.Get<string>("AzureTimeoutStorage.TimeoutManagerDataTableName");
            var connectionString = context.Settings.Get<string>("AzureTimeoutStorage.ConnectionString");
            var catchUpInterval = context.Settings.Get<int>("AzureTimeoutStorage.CatchUpInterval");
            var partitionKeyScope = context.Settings.Get<string>("AzureTimeoutStorage.PartitionKeyScope");
            var endpointName = context.Settings.EndpointName();
            var hostDisplayName = context.Settings.GetOrDefault<string>("NServiceBus.HostInformation.DisplayName");
            var timeoutStateContainerName = context.Settings.GetOrDefault<string>("AzureTimeoutStorage.TimeoutStateContainerName");

            if (createIfNotExist)
            {
                var startupTask = new StartupTask(timeoutDataTableName, connectionString, timeoutManagerDataTableName, timeoutStateContainerName);
                context.RegisterStartupTask(startupTask);
            }

            context.Container.ConfigureComponent(() =>
                new TimeoutPersister(connectionString, timeoutDataTableName, timeoutManagerDataTableName, timeoutStateContainerName, catchUpInterval,
                                     partitionKeyScope, endpointName.ToString(), hostDisplayName),
                DependencyLifecycle.InstancePerCall);
        }

        class StartupTask : FeatureStartupTask
        {
            ILog log = LogManager.GetLogger<StartupTask>();
            string timeoutDataTableName;
            string connectionString;
            string timeoutManagerDataTableName;
            string timeoutStateContainerName;

            public StartupTask(string timeoutDataTableName, string connectionString, string timeoutManagerDataTableName, string timeoutStateContainerName)
            {
                this.timeoutDataTableName = timeoutDataTableName;
                this.connectionString = connectionString;
                this.timeoutManagerDataTableName = timeoutManagerDataTableName;
                this.timeoutStateContainerName = timeoutStateContainerName;
            }

            protected override async Task OnStart(IMessageSession session)
            {
                log.Info("Creating Timeout Table");

                var account = CloudStorageAccount.Parse(connectionString);
                var cloudTableClient = account.CreateCloudTableClient();
                var timeoutTable = cloudTableClient.GetTableReference(timeoutDataTableName);
                await timeoutTable.CreateIfNotExistsAsync().ConfigureAwait(false);

                var timeoutManagerTable = cloudTableClient.GetTableReference(timeoutManagerDataTableName);
                await timeoutManagerTable.CreateIfNotExistsAsync().ConfigureAwait(false);

                var container = account.CreateCloudBlobClient()
                    .GetContainerReference(timeoutStateContainerName);
                await container.CreateIfNotExistsAsync().ConfigureAwait(false);
            }

            protected override Task OnStop(IMessageSession session)
            {
                return Task.FromResult(0);
            }
        }
    }
}