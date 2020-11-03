namespace NServiceBus.Persistence.AzureTable
{
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// Exposes the current <see cref="TableBatchOperation"/> as well the partition key and the table that
    /// are managed by NServiceBus.
    /// </summary>
    public interface IAzureTableStorageSession
    {
        /// <summary>
        /// The table that will be used to store the batched items.
        /// </summary>
        CloudTable Table { get; }

        /// <summary>
        /// The transactional batch that can be used to store items.
        /// </summary>
        /// <remarks>The transactional batch exposed does delay the actual batch operations up to the point when the storage
        /// session is actually committed to avoiding running into transaction timeouts unnecessarily.</remarks>
        TableBatchOperation Batch { get; }

        /// <summary>
        /// The partition key under which all batched items will be stored.
        /// </summary>
        string PartitionKey { get; }
    }
}