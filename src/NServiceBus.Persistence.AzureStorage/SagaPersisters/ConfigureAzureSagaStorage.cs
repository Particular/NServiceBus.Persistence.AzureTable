namespace NServiceBus
{
    using Configuration.AdvanceExtensibility;
    using Persistence;

    /// <summary>
    /// Configuration extensions for the sagas storage
    /// </summary>
    public static class ConfigureAzureSagaStorage
    {
        /// <summary>
        /// Connection string to use for sagas storage.
        /// </summary>
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Sagas> ConnectionString(this PersistenceExtensions<AzureStoragePersistence, StorageType.Sagas> config, string connectionString)
        {
            AzureStorageSagaGuard.CheckConnectionString(connectionString);

            config.GetSettings().Set("AzureSagaStorage.ConnectionString", connectionString);
            return config;
        }

        /// <summary>
        /// Should an attempt be made to create saga storage table or not.
        /// <remarks>Operation will fail if connection string does not allow access to create storage tables</remarks>
        /// </summary>
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Sagas> CreateSchema(this PersistenceExtensions<AzureStoragePersistence, StorageType.Sagas> config, bool createSchema)
        {
            config.GetSettings().Set("AzureSagaStorage.CreateSchema", createSchema);
            return config;
        }
    }
}