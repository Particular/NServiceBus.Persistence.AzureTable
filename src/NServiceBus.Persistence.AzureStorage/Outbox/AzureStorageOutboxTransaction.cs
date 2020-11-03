namespace NServiceBus.Persistence.AzureStorage
{
    using System.Threading.Tasks;
    using Extensibility;
    using Outbox;

    class AzureStorageOutboxTransaction : OutboxTransaction
    {
        public StorageSession StorageSession { get; }

        // By default, store and commit are enabled
        public bool SuppressStoreAndCommit { get; set; }
        public TableEntityPartitionKey? PartitionKey { get; set; }

        public AzureStorageOutboxTransaction(TableHolderResolver resolver, ContextBag context)
        {
            StorageSession = new StorageSession(resolver, context, false);
        }

        public Task Commit()
        {
            return SuppressStoreAndCommit ? Task.CompletedTask : StorageSession.Commit();
        }

        public void Dispose()
        {
            StorageSession.Dispose();
        }
    }
}