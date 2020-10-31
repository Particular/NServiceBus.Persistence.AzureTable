namespace NServiceBus
{
    using Configuration.AdvancedExtensibility;
    using Microsoft.Azure.Cosmos.Table;
    using Persistence.AzureStorage;
    using static Persistence.AzureStorage.Config.WellKnownConfigurationKeys;

    /// <summary>
    /// Configuration extensions for the sagas storage
    /// </summary>
    public static partial class ConfigureAzureSagaStorage
    {
        /// <summary>
        /// Connection string to use for sagas storage.
        /// </summary>
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Sagas> ConnectionString(this PersistenceExtensions<AzureStoragePersistence, StorageType.Sagas> config, string connectionString)
        {
            AzureStorageSagaGuard.CheckConnectionString(connectionString);

            config.GetSettings().Set(SagaStorageConnectionString, connectionString);
            config.GetSettings().Set<IProvideCloudTableClient>(new CloudTableClientFromConnectionString(connectionString));
            return config;
        }

        /// <summary>
        /// Cloud Table Client to use for the saga storage.
        /// </summary>
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Sagas> UseCloudTableClient(this PersistenceExtensions<AzureStoragePersistence, StorageType.Sagas> config, CloudTableClient client)
        {
            Guard.AgainstNull(nameof(client), client);

            var settings = config.GetSettings();
            settings.Set<IProvideCloudTableClient>(new CloudTableClientFromConfiguration(client));

            return config;
        }

        /// <summary>
        /// Should an attempt be made to create saga storage table or not.
        /// <remarks>Operation will fail if connection string does not allow access to create storage tables</remarks>
        /// </summary>
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Sagas> CreateSchema(this PersistenceExtensions<AzureStoragePersistence, StorageType.Sagas> config, bool createSchema)
        {
            Guard.AgainstNull(nameof(config), config);

            config.GetSettings().Set(SagaStorageCreateSchema, createSchema);
            return config;
        }



        /// <summary>
        /// Configures the migration specific settings.
        /// </summary>
        public static MigrationSettings Migration(this PersistenceExtensions<AzureStoragePersistence, StorageType.Sagas> config)
        {
            Guard.AgainstNull(nameof(config), config);

            return new MigrationSettings(config.GetSettings());
        }
    }
}