namespace NServiceBus.Persistence.AzureStorage
{
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    ///
    /// </summary>
    public interface IAzureStorageStorageSession
    {
        /// <summary>
        ///
        /// </summary>
        CloudTable Table { get; }

        /// <summary>
        ///
        /// </summary>
        TableBatchOperation Batch { get; }
    }
}