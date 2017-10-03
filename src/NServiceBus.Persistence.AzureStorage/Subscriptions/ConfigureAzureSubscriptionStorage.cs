namespace NServiceBus
{
    using System;
    using Configuration.AdvancedExtensibility;
    using Subscriptions;
    using static Persistence.AzureStorage.Config.WellKnownConfigurationKeys;

    /// <summary>
    /// Configuration extensions for the subscription storage
    /// </summary>
    public static class ConfigureAzureSubscriptionStorage
    {
        /// <summary>
        /// Connection string to use for subscriptions storage.
        /// </summary>
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Subscriptions> ConnectionString(this PersistenceExtensions<AzureStoragePersistence, StorageType.Subscriptions> config, string connectionString)
        {
            AzureSubscriptionStorageGuard.CheckConnectionString(connectionString);

            config.GetSettings().Set(SubscriptionStorageConnectionString, connectionString);
            return config;
        }

        /// <summary>
        /// Table name to create in Azure storage account to persist subscriptions.
        /// </summary>
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Subscriptions> TableName(this PersistenceExtensions<AzureStoragePersistence, StorageType.Subscriptions> config, string tableName)
        {
            AzureSubscriptionStorageGuard.CheckTableName(tableName);

            config.GetSettings().Set(SubscriptionStorageTableName, tableName);
            return config;
        }

        /// <summary>
        /// Cache subscriptions for a given <see cref="TimeSpan"/>.
        /// </summary>
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Subscriptions> CacheFor(this PersistenceExtensions<AzureStoragePersistence, StorageType.Subscriptions> config, TimeSpan timeSpan)
        {
            AzureSubscriptionStorageGuard.AgainstNegativeAndZero(nameof(timeSpan), timeSpan);
            config.GetSettings().Set(SubscriptionStorageCacheFor, timeSpan);
            return config;
        }

        /// <summary>
        /// Should an attempt at startup be made to verify if subscriptions storage table exists or not and if not create it.
        /// <remarks>Operation will fail if connection string does not allow access to create storage tables</remarks>
        /// </summary>
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Subscriptions> CreateSchema(this PersistenceExtensions<AzureStoragePersistence, StorageType.Subscriptions> config, bool createSchema)
        {
            config.GetSettings().Set(SubscriptionStorageTableName, createSchema);
            return config;
        }
    }
}