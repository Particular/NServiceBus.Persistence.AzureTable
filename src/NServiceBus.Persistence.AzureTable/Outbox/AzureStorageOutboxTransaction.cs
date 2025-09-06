namespace NServiceBus.Persistence.AzureTable
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Outbox;

    sealed class AzureStorageOutboxTransaction(TableClientHolderResolver resolver, ContextBag context)
        : IOutboxTransaction
    {
        public StorageSession StorageSession { get; } = new(resolver, context);

        // By default, store and commit are enabled
        public bool SuppressStoreAndCommit { get; set; }

        public TableEntityPartitionKey? PartitionKey { get; set; }

        public Task Commit(CancellationToken cancellationToken = default)
            => SuppressStoreAndCommit ? Task.CompletedTask : StorageSession.Commit(cancellationToken);

        public void Dispose() => StorageSession.Dispose();
        public ValueTask DisposeAsync() => StorageSession.DisposeAsync();
    }
}