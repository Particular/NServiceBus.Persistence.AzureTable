namespace NServiceBus
{
    using Configuration.AdvancedExtensibility;
    using Microsoft.Azure.Cosmos.Table;
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
            settings.Set<IProvideCloudTableClient>(new CloudTableClientFromConnectionString(connectionString));
            settings.Set<IProvideCloudTableClientForSubscriptions>(new CloudTableClientForSubscriptionsFromConnectionString(connectionString));

            return config;
        }

        /// <summary>
        /// Cloud Table Client to use for azure Saga, Outbox and Subscription storage.
        /// </summary>
        public static PersistenceExtensions<AzureTablePersistence> UseCloudTableClient(this PersistenceExtensions<AzureTablePersistence> config, CloudTableClient client)
        {
            Guard.AgainstNull(nameof(client), client);

            var settings = config.GetSettings();
            settings.Set<IProvideCloudTableClient>(new CloudTableClientFromConfiguration(client));
            settings.Set<IProvideCloudTableClientForSubscriptions>(new CloudTableClientForSubscriptionsFromConfiguration(client));

            return config;
        }

        /// <summary>
        /// Sets the default table name that will be used.
        /// </summary>
        /// <remarks>When the default table is not set the table information needs to be provided as part of the message handling pipeline.</remarks>
        public static PersistenceExtensions<AzureTablePersistence> DefaultTable(this PersistenceExtensions<AzureTablePersistence> persistenceExtensions, string tableName)
        {
            Guard.AgainstNull(nameof(persistenceExtensions), persistenceExtensions);

            persistenceExtensions.GetSettings().Set(new TableInformation(tableName));

            return persistenceExtensions;
        }
    }
}