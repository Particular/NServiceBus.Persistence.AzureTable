namespace NServiceBus
{
    using Configuration.AdvanceExtensibility;
    using Persistence.AzureStorage.Config;

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
        public static PersistenceExtensions<AzureStoragePersistence> ConnectionString(this PersistenceExtensions<AzureStoragePersistence> config, string connectionString)
        {
            AzureStorageSagaGuard.CheckConnectionString(connectionString);

            var settings = config.GetSettings();
            settings.Set(WellKnownConfigurationKeys.SagaStorageConnectionString, connectionString);
            settings.Set(WellKnownConfigurationKeys.SubscriptionStorageConnectionString, connectionString);
            settings.Set(WellKnownConfigurationKeys.TimeoutStorageConnectionString, connectionString);

            return config;
        }
    }
}