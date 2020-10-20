#pragma warning disable 1591

namespace NServiceBus
{
    using System;

    [ObsoleteEx(Message = "Azure transports support timeouts natively and do not require timeout persistence.",
        TreatAsErrorFromVersion = "3",
        RemoveInVersion = "4")]
    public class AzureStorageTimeoutPersistence
    {
    }

    public static class ConfigureAzureTimeoutStorage
    {
        const string ObsoleteMessage = "Azure transports support timeouts natively and do not require timeout persistence.";

        [ObsoleteEx(Message = ObsoleteMessage,
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4")]
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> ConnectionString(this PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> config, string connectionString)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(Message = ObsoleteMessage,
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4")]
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> TimeoutStateContainerName(this PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> config, string blobName)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(Message = ObsoleteMessage,
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4")]
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> CreateSchema(this PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> config, bool createSchema)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(Message = ObsoleteMessage,
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4")]
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> TimeoutManagerDataTableName(this PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> config, string tableName)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(Message = ObsoleteMessage,
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4")]
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> TimeoutDataTableName(this PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> config, string tableName)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(Message = ObsoleteMessage,
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4")]
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> CatchUpInterval(this PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> config, int catchUpInterval)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(Message = ObsoleteMessage,
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4")]
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> PartitionKeyScope(this PersistenceExtensions<AzureStoragePersistence, StorageType.Timeouts> config, string partitionKeyScope)
        {
            throw new NotImplementedException();
        }
    }
}
#pragma warning restore 1591
