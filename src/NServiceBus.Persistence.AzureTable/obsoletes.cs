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
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Timeouts> ConnectionString(this PersistenceExtensions<AzureTablePersistence, StorageType.Timeouts> config, string connectionString)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(Message = ObsoleteMessage,
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4")]
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Timeouts> TimeoutStateContainerName(this PersistenceExtensions<AzureTablePersistence, StorageType.Timeouts> config, string blobName)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(Message = ObsoleteMessage,
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4")]
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Timeouts> CreateSchema(this PersistenceExtensions<AzureTablePersistence, StorageType.Timeouts> config, bool createSchema)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(Message = ObsoleteMessage,
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4")]
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Timeouts> TimeoutManagerDataTableName(this PersistenceExtensions<AzureTablePersistence, StorageType.Timeouts> config, string tableName)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(Message = ObsoleteMessage,
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4")]
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Timeouts> TimeoutDataTableName(this PersistenceExtensions<AzureTablePersistence, StorageType.Timeouts> config, string tableName)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(Message = ObsoleteMessage,
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4")]
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Timeouts> CatchUpInterval(this PersistenceExtensions<AzureTablePersistence, StorageType.Timeouts> config, int catchUpInterval)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(Message = ObsoleteMessage,
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4")]
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Timeouts> PartitionKeyScope(this PersistenceExtensions<AzureTablePersistence, StorageType.Timeouts> config, string partitionKeyScope)
        {
            throw new NotImplementedException();
        }
    }

    public static partial class ConfigureAzureSagaStorage
    {
        [ObsoleteEx(Message = "The migration mode that supports looking up correlated sagas by secondary indexes is by default enabled and assumes no full table scan is required. In order to opt-in for a table scan for sagas stored with version 1.4 or earlier use `AllowSecondaryKeyLookupToFallbackToFullTableScan`",
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4")]
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> AssumeSecondaryIndicesExist(this PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(Message = "The table creation is enabled when the installers are enabled. In order to disable table creation either remove `endpointConfiguration.EnableInstallers()` or opt-out from table creation by calling `DisableTableCreation`. Table creation at runtime without installers enabled is no longer supported. The tables required at runtime when the installers are disabled need to be created during via operational scripting. Consolidate the upgrade guide for more details.",
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4")]
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> CreateSchema(this PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> config, bool createSchema)
        {
            throw new NotImplementedException();
        }
    }

    public static partial class ConfigureAzureSubscriptionStorage
    {
        [ObsoleteEx(Message = "The table creation is enabled when the installers are enabled. In order to disable table creation either remove `endpointConfiguration.EnableInstallers()` or opt-out from table creation by calling `DisableTableCreation`. Table creation at runtime without installers enabled is no longer supported. The tables required at runtime when the installers are disabled need to be created during via operational scripting. Consolidate the upgrade guide for more details.",
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4")]
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Subscriptions> CreateSchema(this PersistenceExtensions<AzureTablePersistence, StorageType.Subscriptions> config, bool createSchema)
        {
            throw new NotImplementedException();
        }
    }
}
#pragma warning restore 1591
