namespace NServiceBus
{
    using Azure;
    using Config;
    using Features;
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

            var account = CloudStorageAccount.Parse(connectionString);

            if (createIfNotExist)
            {
                var timeoutTable = account.CreateCloudTableClient().GetTableReference(timeoutDataTableName);
                timeoutTable.CreateIfNotExists();

                var timeoutManagerTable = account.CreateCloudTableClient().GetTableReference(timeoutManagerDataTableName);
                timeoutManagerTable.CreateIfNotExists();

                var container = account.CreateCloudBlobClient().GetContainerReference(timeoutStateContainerName);
                container.CreateIfNotExists();
            }

            context.Container.ConfigureComponent(()=>
                new TimeoutPersister(connectionString, timeoutDataTableName, timeoutManagerDataTableName, timeoutStateContainerName, catchUpInterval, 
                                     partitionKeyScope, endpointName, hostDisplayName), 
                DependencyLifecycle.InstancePerCall);
        }
    }
}