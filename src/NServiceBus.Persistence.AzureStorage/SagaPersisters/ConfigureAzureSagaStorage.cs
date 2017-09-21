namespace NServiceBus
{
    using Configuration.AdvancedExtensibility;
    using Persistence;
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
            return config;
        }

        /// <summary>
        /// Should an attempt be made to create saga storage table or not.
        /// <remarks>Operation will fail if connection string does not allow access to create storage tables</remarks>
        /// </summary>
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Sagas> CreateSchema(this PersistenceExtensions<AzureStoragePersistence, StorageType.Sagas> config, bool createSchema)
        {
            config.GetSettings().Set(SagaStorageCreateSchema, createSchema);
            return config;
        }

        /// <summary>
        /// Confirm that all sagas have secondary indices to opt-out from full table scanning upon new saga creation.
        /// <remarks>Sagas created with NServiceBus.Persistence.AzureStorage NuGet package have secondary indices by default.
        /// Sagas created with NServiceBus.Azure NuGet package need to be migrated using upgrade guides provided on our documentation site.</remarks>
        /// <param name="config"></param>
        /// <param name="assumeSecondaryIndecesExist"></param>
        /// </summary>
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Sagas> AssumeSecondaryIndicesExist(this PersistenceExtensions<AzureStoragePersistence, StorageType.Sagas> config, bool assumeSecondaryIndecesExist)
        {
            config.GetSettings().Set(SagaStorageAssumeSecondaryIndicesExist, assumeSecondaryIndecesExist);
            return config;
        }

    }
}