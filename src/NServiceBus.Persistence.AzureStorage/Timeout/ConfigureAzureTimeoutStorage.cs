namespace NServiceBus
{
    using Configuration.AdvancedExtensibility;
    using Timeout;
    using static Persistence.AzureStorage.Config.WellKnownConfigurationKeys;

    /// <summary>
    /// Configuration extensions for the subscription storage
    /// </summary>
    public static class ConfigureAzureTimeoutStorage
    {
        /// <summary>
        /// Connection string to use for timeouts storage.
        /// </summary>
        [ObsoleteEx(Message = "Azure transports support timeouts natively and do not require timeout persistence. For Azure Storage Queues transport refer to the delayed delivery API.",
            ReplacementTypeOrMember = "EndpointConfiguration.UseTransport<AzureStorageQueueTransport>().DelayedDelivery()",
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4")]
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> ConnectionString(this PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> config, string connectionString)
        {
            AzureTimeoutStorageGuard.CheckConnectionString(connectionString);

            config.GetSettings().Set(TimeoutStorageConnectionString, connectionString);
            return config;
        }

        /// <summary></summary>
        [ObsoleteEx(Message = "Azure transports support timeouts natively and do not require timeout persistence. For Azure Storage Queues transport refer to the delayed delivery API.",
            ReplacementTypeOrMember = "EndpointConfiguration.UseTransport<AzureStorageQueueTransport>().DelayedDelivery()",
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4")]
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> TimeoutStateContainerName(this PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> config, string blobName)
        {
            config.GetSettings().Set(TimeoutStorageTimeoutStateContainerName, blobName);
            return config;
        }

        /// <summary>
        /// Should an attempt at startup be made to verify if storage tables for timeouts exist or not and if not create those.
        /// <remarks>Operation will fail if connection string does not allow access to create storage tables</remarks>
        /// </summary>
        [ObsoleteEx(Message = "Azure transports support timeouts natively and do not require timeout persistence. For Azure Storage Queues transport refer to the delayed delivery API.",
            ReplacementTypeOrMember = "EndpointConfiguration.UseTransport<AzureStorageQueueTransport>().DelayedDelivery()",
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4")]
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> CreateSchema(this PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> config, bool createSchema)
        {
            config.GetSettings().Set(TimeoutStorageCreateSchema, createSchema);
            return config;
        }

        /// <summary>
        /// Set the name of the table where the timeout manager stores it's internal state.
        /// </summary>
        [ObsoleteEx(Message = "Azure transports support timeouts natively and do not require timeout persistence. For Azure Storage Queues transport refer to the delayed delivery API.",
            ReplacementTypeOrMember = "EndpointConfiguration.UseTransport<AzureStorageQueueTransport>().DelayedDelivery()",
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4")]
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> TimeoutManagerDataTableName(this PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> config, string tableName)
        {
            AzureTimeoutStorageGuard.CheckTableName(tableName);

            config.GetSettings().Set(TimeoutStorageTimeoutManagerDataTableName, tableName);
            return config;
        }

        /// <summary>
        ///  Set the name of the table where the timeouts themselves are stored.
        /// </summary>
        [ObsoleteEx(Message = "Azure transports support timeouts natively and do not require timeout persistence. For Azure Storage Queues transport refer to the delayed delivery API.",
            ReplacementTypeOrMember = "EndpointConfiguration.UseTransport<AzureStorageQueueTransport>().DelayedDelivery()",
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4")]
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> TimeoutDataTableName(this PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> config, string tableName)
        {
            AzureTimeoutStorageGuard.CheckTableName(tableName);

            config.GetSettings().Set(TimeoutStorageTimeoutDataTableName, tableName);
            return config;
        }

        /// <summary>
        ///  Set the catchup interval in seconds for missed timeouts.
        /// </summary>
        /// <param name="catchUpInterval">Catch up interval in seconds</param>
        /// <param name="config"></param>
        [ObsoleteEx(Message = "Azure transports support timeouts natively and do not require timeout persistence. For Azure Storage Queues transport refer to the delayed delivery API.",
            ReplacementTypeOrMember = "EndpointConfiguration.UseTransport<AzureStorageQueueTransport>().DelayedDelivery()",
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4")]
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> CatchUpInterval(this PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> config, int catchUpInterval)
        {
            AzureTimeoutStorageGuard.CheckCatchUpInterval(catchUpInterval);

            config.GetSettings().Set(TimeoutStorageCatchUpInterval, catchUpInterval);
            return config;
        }

        /// <summary>
        ///  Time range used as partition key value for all timeouts.
        /// </summary>
        /// <param name="partitionKeyScope">Partition key DateTime format string.</param>
        /// <param name="config"></param>
        /// <remarks>For optimal performance, this should be in line with the CatchUpInterval.</remarks>
        [ObsoleteEx(Message = "Azure transports support timeouts natively and do not require timeout persistence. For Azure Storage Queues transport refer to the delayed delivery API.",
            ReplacementTypeOrMember = "EndpointConfiguration.UseTransport<AzureStorageQueueTransport>().DelayedDelivery()",
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4")]
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> PartitionKeyScope(this PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> config, string partitionKeyScope)
        {
            AzureTimeoutStorageGuard.CheckPartitionKeyScope(partitionKeyScope);

            config.GetSettings().Set(TimeoutStoragePartitionKeyScope, partitionKeyScope);
            return config;
        }
    }
}