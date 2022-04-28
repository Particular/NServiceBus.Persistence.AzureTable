namespace NServiceBus.Persistence.AzureTable
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Outbox;
    using Transport;

    class SynchronizedStorageSession : ICompletableSynchronizedStorageSession
    {
        public SynchronizedStorageSession(TableHolderResolver tableHolderResolver, CurrentSharedTransactionalBatchHolder currentSharedTransactionalBatchHolder)
        {
            this.tableHolderResolver = tableHolderResolver;
            this.currentSharedTransactionalBatchHolder = currentSharedTransactionalBatchHolder;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                Session.Dispose();
                disposed = true;
            }
        }

        public ValueTask<bool> TryOpen(IOutboxTransaction transaction, ContextBag context, CancellationToken cancellationToken = default)
        {
            if (transaction is AzureStorageOutboxTransaction azureStorageOutboxTransaction)
            {
                Session = azureStorageOutboxTransaction.StorageSession;
                ownsTransaction = false;
                return new ValueTask<bool>(true);
            }
            return new ValueTask<bool>(false);
        }

        public ValueTask<bool> TryOpen(TransportTransaction transportTransaction, ContextBag context,
                                       CancellationToken cancellationToken = default) =>
            new ValueTask<bool>(false);

        public Task Open(ContextBag contextBag, CancellationToken cancellationToken = default)
        {
            ownsTransaction = true;
            Session = new StorageSession(tableHolderResolver, contextBag);
            currentSharedTransactionalBatchHolder.SetCurrent(Session);
            return Task.CompletedTask;
        }

        public Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            if (ownsTransaction)
            {
                return Session.Commit(cancellationToken);
            }
            return Task.CompletedTask;
        }

        readonly TableHolderResolver tableHolderResolver;
        readonly CurrentSharedTransactionalBatchHolder currentSharedTransactionalBatchHolder;
        bool disposed;
        public StorageSession Session { get; private set; }
        bool ownsTransaction;
    }
}