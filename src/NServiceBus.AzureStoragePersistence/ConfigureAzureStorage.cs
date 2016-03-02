namespace NServiceBus
{
    using SagaPersisters;
    using Configuration.AdvanceExtensibility;

    /// <summary>
    /// Configuration extensions for all Azure storage settings
    /// </summary>
    public static class ConfigureAzureStorage
    {
        /// <summary>
        /// Connection string to use for azure Saga, Timeout and Subscription storage.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <returns></returns>
        public static PersistenceExtentions<AzureStoragePersistence> ConnectionString(this PersistenceExtentions<AzureStoragePersistence> config, string connectionString)
        {
            AzureStorageSagaGuard.CheckConnectionString(connectionString);

            config.GetSettings().Set("AzureSagaStorage.ConnectionString", connectionString);
            config.GetSettings().Set("AzureSubscriptionStorage.ConnectionString", connectionString);
            config.GetSettings().Set("AzureTimeoutStorage.ConnectionString", connectionString);

            return config;
        }
    }
}