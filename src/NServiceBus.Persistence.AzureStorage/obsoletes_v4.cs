namespace NServiceBus
{
    using System;
    using Features;

    /// <summary></summary>
    [ObsoleteEx(Message = "Azure Storage Queues supports timeouts natively and does not require timeout persistence.",
        TreatAsErrorFromVersion = "3",
        RemoveInVersion = "4")]
    public class AzureStorageTimeoutPersistence : Feature
    {
        internal AzureStorageTimeoutPersistence()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Configuration extensions for the subscription storage
    /// </summary>
    public static class ConfigureAzureTimeoutStorage
    {
        const string ObsoleteMessage = "Azure Storage Queues transport supports timeouts natively and does not require timeout persistence. Refer to the delayed delivery API.";
        const string ReplacementTypeOrMember = "EndpointConfiguration.UseTransport<AzureStorageQueueTransport>().DelayedDelivery()";

        /// <summary>
        /// Connection string to use for timeouts storage.
        /// </summary>
        [ObsoleteEx(Message = ObsoleteMessage,
            ReplacementTypeOrMember = ReplacementTypeOrMember,
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4")]
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> ConnectionString(this PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> config, string connectionString)
        {
            throw new NotImplementedException();
        }

        /// <summary></summary>
        [ObsoleteEx(Message = ObsoleteMessage,
            ReplacementTypeOrMember = ReplacementTypeOrMember,
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4")]
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> TimeoutStateContainerName(this PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> config, string blobName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Should an attempt at startup be made to verify if storage tables for timeouts exist or not and if not create those.
        /// <remarks>Operation will fail if connection string does not allow access to create storage tables</remarks>
        /// </summary>
        [ObsoleteEx(Message = ObsoleteMessage,
            ReplacementTypeOrMember = ReplacementTypeOrMember,
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4")]
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> CreateSchema(this PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> config, bool createSchema)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Set the name of the table where the timeout manager stores it's internal state.
        /// </summary>
        [ObsoleteEx(Message = ObsoleteMessage,
            ReplacementTypeOrMember = ReplacementTypeOrMember,
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4")]
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> TimeoutManagerDataTableName(this PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> config, string tableName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///  Set the name of the table where the timeouts themselves are stored.
        /// </summary>
        [ObsoleteEx(Message = ObsoleteMessage,
            ReplacementTypeOrMember = ReplacementTypeOrMember,
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4")]
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> TimeoutDataTableName(this PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> config, string tableName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///  Set the catchup interval in seconds for missed timeouts.
        /// </summary>
        /// <param name="catchUpInterval">Catch up interval in seconds</param>
        /// <param name="config"></param>
        [ObsoleteEx(Message = ObsoleteMessage,
            ReplacementTypeOrMember = ReplacementTypeOrMember,
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4")]
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> CatchUpInterval(this PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> config, int catchUpInterval)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///  Time range used as partition key value for all timeouts.
        /// </summary>
        /// <param name="partitionKeyScope">Partition key DateTime format string.</param>
        /// <param name="config"></param>
        /// <remarks>For optimal performance, this should be in line with the CatchUpInterval.</remarks>
        [ObsoleteEx(Message = ObsoleteMessage,
            ReplacementTypeOrMember = ReplacementTypeOrMember,
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4")]
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> PartitionKeyScope(this PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> config, string partitionKeyScope)
        {
            throw new NotImplementedException();
        }
    }
}