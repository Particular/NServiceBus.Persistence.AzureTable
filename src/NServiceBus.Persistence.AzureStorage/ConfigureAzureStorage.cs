namespace NServiceBus
{
    using Configuration.AdvancedExtensibility;
    using static Persistence.AzureStorage.Config.WellKnownConfigurationKeys;

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
        public static PersistenceExtensions<AzureStoragePersistence> ConnectionString(this PersistenceExtensions<AzureStoragePersistence> config, string connectionString)
        {
            AzureStorageSagaGuard.CheckConnectionString(connectionString);

            var settings = config.GetSettings();
            settings.Set(SagaStorageConnectionString, connectionString);
            settings.Set<IProvideCloudTableClient>(new CloudTableClientFromConnectionString(connectionString));
            settings.Set(SubscriptionStorageConnectionString, connectionString);
            settings.Set<IProvideCloudTableClientForSubscriptions>(new CloudTableClientForSubscriptionsFromConnectionString(connectionString));

            return config;
        }
    }
}