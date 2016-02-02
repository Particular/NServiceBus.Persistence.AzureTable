namespace NServiceBus
{
    using Config;
    using Features;
    using Microsoft.WindowsAzure.Storage;
    using Unicast.Subscriptions;

    public class AzureStorageSubscriptionPersistence : Feature
    {
        internal AzureStorageSubscriptionPersistence()
        {
            DependsOn<MessageDrivenSubscriptions>();
            Defaults(s =>
            {
                var configSection = s.GetConfigSection<AzureSubscriptionStorageConfig>() ?? new AzureSubscriptionStorageConfig();
                s.SetDefault("AzureSubscriptionStorage.ConnectionString", configSection.ConnectionString);
                s.SetDefault("AzureSubscriptionStorage.TableName", configSection.TableName);
                s.SetDefault("AzureSubscriptionStorage.CreateSchema", configSection.CreateSchema);
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

            var account = CloudStorageAccount.Parse(connectionString);

            var table = account.CreateCloudTableClient().GetTableReference(subscriptionTableName);
            if (createIfNotExist) table.CreateIfNotExists();

            context.Container.ConfigureComponent(() => new AzureSubscriptionStorage(subscriptionTableName, connectionString), DependencyLifecycle.InstancePerCall);
        }
    }
}