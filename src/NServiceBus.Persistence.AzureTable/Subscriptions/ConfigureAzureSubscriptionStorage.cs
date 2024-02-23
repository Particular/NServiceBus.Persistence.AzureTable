namespace NServiceBus
{
    using System;
    using Azure.Data.Tables;
    using Configuration.AdvancedExtensibility;
    using Persistence.AzureTable;

    /// <summary>
    /// Configuration extensions for the subscription storage
    /// </summary>
    public static partial class ConfigureAzureSubscriptionStorage
    {
        /// <summary>
        /// Connection string to use for subscriptions storage.
        /// </summary>
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Subscriptions> ConnectionString(this PersistenceExtensions<AzureTablePersistence, StorageType.Subscriptions> config, string connectionString)
        {
            AzureSubscriptionStorageGuard.CheckConnectionString(connectionString);

            config.GetSettings().Set<IProvideTableServiceClientForSubscriptions>(new TableServiceClientForSubscriptionsFromConnectionString(connectionString));
            return config;
        }

        /// <summary>
        /// TableServiceClient to use for the Subscription storage.
        /// </summary>
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Subscriptions> UseTableServiceClient(this PersistenceExtensions<AzureTablePersistence, StorageType.Subscriptions> config, TableServiceClient client)
        {
            ArgumentNullException.ThrowIfNull(client);

            var settings = config.GetSettings();
            settings.Set<IProvideTableServiceClientForSubscriptions>(new TableServiceServiceClientForSubscriptionsFromConfiguration(client));

            return config;
        }

        /// <summary>
        /// Table name to create in Azure storage account to persist subscriptions.
        /// </summary>
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Subscriptions> TableName(this PersistenceExtensions<AzureTablePersistence, StorageType.Subscriptions> config, string tableName)
        {
            AzureSubscriptionStorageGuard.CheckTableName(tableName);

            config.GetSettings().Set(WellKnownConfigurationKeys.SubscriptionStorageTableName, tableName);
            return config;
        }

        /// <summary>
        /// Cache subscriptions for a given <see cref="TimeSpan"/>.
        /// </summary>
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Subscriptions> CacheFor(this PersistenceExtensions<AzureTablePersistence, StorageType.Subscriptions> config, TimeSpan timeSpan)
        {
            AzureSubscriptionStorageGuard.AgainstNegativeAndZero(nameof(timeSpan), timeSpan);
            config.GetSettings().Set(WellKnownConfigurationKeys.SubscriptionStorageCacheFor, timeSpan);
            return config;
        }

        /// <summary>
        /// Disables the table creation for the subscription storage.
        /// </summary>
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Subscriptions> DisableTableCreation(this PersistenceExtensions<AzureTablePersistence, StorageType.Subscriptions> config)
        {
            ArgumentNullException.ThrowIfNull(config);

            var settings = config.GetSettings();
            settings.GetOrCreate<SubscriptionStorageInstallerSettings>().Disabled = true;

            return config;
        }
    }
}