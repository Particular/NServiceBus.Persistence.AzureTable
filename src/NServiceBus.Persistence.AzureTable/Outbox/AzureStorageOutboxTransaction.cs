namespace NServiceBus.Persistence.AzureTable
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Outbox;

    class AzureStorageOutboxTransaction : IOutboxTransaction
    {
        public StorageSession StorageSession { get; }

        // By default, store and commit are enabled
        public bool SuppressStoreAndCommit { get; set; }
        public TableEntityPartitionKey? PartitionKey { get; set; }

        public AzureStorageOutboxTransaction(TableHolderResolver resolver, ContextBag context)
        {
            StorageSession = new StorageSession(resolver, context);
        }

        public Task Commit(CancellationToken cancellationToken = default)
        {
            return SuppressStoreAndCommit ? Task.CompletedTask : StorageSession.Commit(cancellationToken);
        }

        public void Dispose()
        {
            StorageSession.Dispose();
        }
    }
}