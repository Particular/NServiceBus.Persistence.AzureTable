namespace NServiceBus
{
    using System;
    using Azure.Data.Tables;
    using Configuration.AdvancedExtensibility;
    using Persistence.AzureTable;

    /// <summary>
    /// Configuration extensions for all Azure storage settings
    /// </summary>
    public static class ConfigureAzureStorage
    {
        /// <summary>
        /// Connection string to use for azure Saga, Outbox and Subscription storage.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="connectionString">The connection string.</param>
        public static PersistenceExtensions<AzureTablePersistence> ConnectionString(this PersistenceExtensions<AzureTablePersistence> config, string connectionString)
        {
            AzureStorageSagaGuard.CheckConnectionString(connectionString);

            var settings = config.GetSettings();
            settings.Set<IProvideTableServiceClient>(new TableServiceClientFromConnectionString(connectionString));
            settings.Set<IProvideTableServiceClientForSubscriptions>(new TableServiceClientForSubscriptionsFromConnectionString(connectionString));

            return config;
        }

        /// <summary>
        /// TableServiceClient to use for Saga, Outbox and Subscription storage.
        /// </summary>
        public static PersistenceExtensions<AzureTablePersistence> UseTableServiceClient(this PersistenceExtensions<AzureTablePersistence> config, TableServiceClient client)
        {
            ArgumentNullException.ThrowIfNull(client);

            var settings = config.GetSettings();
            settings.Set<IProvideTableServiceClient>(new TableServiceClientFromConfiguration(client));
            settings.Set<IProvideTableServiceClientForSubscriptions>(new TableServiceServiceClientForSubscriptionsFromConfiguration(client));

            return config;
        }

        /// <summary>
        /// Sets the default table name that will be used for Saga, Outbox and Subscription storage.
        /// </summary>
        /// <remarks>When the default table is not set the table information needs to be provided as part of the message handling pipeline.</remarks>
        public static PersistenceExtensions<AzureTablePersistence> DefaultTable(this PersistenceExtensions<AzureTablePersistence> config, string tableName)
        {
            ArgumentNullException.ThrowIfNull(config);

            config.GetSettings().Set(new TableInformation(tableName));

            return config;
        }

        /// <summary>
        /// Disables the table creation for Saga, Outbox and Subscription storage.
        /// </summary>
        public static PersistenceExtensions<AzureTablePersistence> DisableTableCreation(this PersistenceExtensions<AzureTablePersistence> config)
        {
            ArgumentNullException.ThrowIfNull(config);

            var settings = config.GetSettings();
            settings.GetOrCreate<SynchronizedStorageInstallerSettings>().Disabled = true;
            settings.GetOrCreate<SubscriptionStorageInstallerSettings>().Disabled = true;

            return config;
        }
    }
}