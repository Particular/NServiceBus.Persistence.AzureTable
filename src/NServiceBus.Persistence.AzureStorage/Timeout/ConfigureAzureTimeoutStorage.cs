﻿namespace NServiceBus
{
    using Configuration.AdvanceExtensibility;
    using Persistence;
    using Persistence.AzureStorage.Config;
    using Timeout;

    /// <summary>
    /// Configuration extensions for the subscription storage
    /// </summary>
    public static class ConfigureAzureTimeoutStorage
    {
        /// <summary>
        /// Connection string to use for timeouts storage.
        /// </summary>
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> ConnectionString(this PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> config, string connectionString)
        {
            AzureTimeoutStorageGuard.CheckConnectionString(connectionString);

            config.GetSettings().Set(WellKnownConfigurationKeys.TimeoutStorageConnectionString, connectionString);
            return config;
        }

        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> TimeoutStateContainerName(this PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> config, string blobName)
        {
            config.GetSettings().Set(WellKnownConfigurationKeys.TimeoutStorageTimeoutStateContainerName, blobName);
            return config;
        }

        /// <summary>
        /// Should an attempt at startup be made to verify if storage tables for timeouts exist or not and if not create those.
        /// <remarks>Operation will fail if connection string does not allow access to create storage tables</remarks>
        /// </summary>
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> CreateSchema(this PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> config, bool createSchema)
        {
            config.GetSettings().Set(WellKnownConfigurationKeys.TimeoutStorageCreateSchema, createSchema);
            return config;
        }

        /// <summary>
        /// Set the name of the table where the timeout manager stores it's internal state.
        /// </summary>
        // TODO: needs to be obsoleted
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> TimeoutManagerDataTableName(this PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> config, string tableName)
        {
            AzureTimeoutStorageGuard.CheckTableName(tableName);

            config.GetSettings().Set("AzureTimeoutStorage.TimeoutManagerDataTableName", tableName);
            return config;
        }

        /// <summary>
        ///  Set the name of the table where the timeouts themselves are stored.
        /// </summary>
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> TimeoutDataTableName(this PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> config, string tableName)
        {
            AzureTimeoutStorageGuard.CheckTableName(tableName);

            config.GetSettings().Set(WellKnownConfigurationKeys.TimeoutStorageTimeoutDataTableName, tableName);
            return config;
        }

        /// <summary>
        ///  Set the catchup interval in seconds for missed timeouts.
        /// </summary>
        /// <param name="catchUpInterval">Catch up interval in seconds</param>
        /// <param name="config"></param>
        // TODO: needs to be obsoleted
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> CatchUpInterval(this PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> config, int catchUpInterval)
        {
            AzureTimeoutStorageGuard.CheckCatchUpInterval(catchUpInterval);

            config.GetSettings().Set("AzureTimeoutStorage.CatchUpInterval", catchUpInterval);
            return config;
        }

        /// <summary>
        ///  Time range used as partition key value for all timeouts.
        /// </summary>
        /// <param name="partitionKeyScope">Partition key DateTime format string.</param>
        /// <param name="config"></param>
        /// <remarks>For optimal performance, this should be in line with the CatchUpInterval.</remarks>
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> PartitionKeyScope(this PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> config, string partitionKeyScope)
        {
            AzureTimeoutStorageGuard.CheckPartitionKeyScope(partitionKeyScope);

            config.GetSettings().Set(WellKnownConfigurationKeys.TimeoutStoragePartitionKeyScope, partitionKeyScope);
            return config;
        }
    }
}