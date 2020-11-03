namespace NServiceBus.Testing
{
    using Microsoft.Azure.Cosmos.Table;
    using Extensibility;
    using Persistence;
    using Persistence.AzureStorage;

    /// <summary>
    /// A fake implementation for <see cref="SynchronizedStorageSession"/> for testing purposes.
    /// </summary>
    public class TestableAzureStorageStorageSession : SynchronizedStorageSession, IWorkWithSharedTransactionalBatch
    {
        /// <summary>
        /// Initializes a new TestableCosmosSynchronizedStorageSession with a partition key.
        /// </summary>
        public TestableAzureStorageStorageSession(TableEntityPartitionKey partitionKey)
        {
            var contextBag = new ContextBag();
            contextBag.Set(partitionKey);
            ((IWorkWithSharedTransactionalBatch) this).CurrentContextBag = contextBag;
            PartitionKey = partitionKey.PartitionKey;
        }

        ContextBag IWorkWithSharedTransactionalBatch.CurrentContextBag { get; set; }

        /// <summary>
        /// The cloud table to be used.
        /// </summary>
        public CloudTable Table { get; set; }

        /// <summary>
        /// The batch to be used.
        /// </summary>
        public TableBatchOperation Batch { get; set; }

        /// <summary>
        /// The partition key to be used.
        /// </summary>
        public string PartitionKey { get; }
    }
}