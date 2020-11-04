#pragma warning disable 1591, 0612, 0618, 0612
namespace NServiceBus
{
    using System;

    [ObsoleteEx(Message = "Azure transports support timeouts natively and do not require timeout persistence.",
        TreatAsErrorFromVersion = "3",
        RemoveInVersion = "4")]
    public class AzureStorageTimeoutPersistence
    {
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
    }
}
#pragma warning restore 1591
