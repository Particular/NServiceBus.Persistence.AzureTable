namespace NServiceBus.Persistence.AzureTable
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Microsoft.Azure.Cosmos.Table;
    using Outbox;
    using Transport;

    class AzureStorageSynchronizedStorageSession : ICompletableSynchronizedStorageSession, IWorkWithSharedTransactionalBatch
    {
        public AzureStorageSynchronizedStorageSession(TableHolderResolver tableHolderResolver)
        {
            this.tableHolderResolver = tableHolderResolver;
        }

        public void Dispose()
        {
            if (!disposed && ownsTransaction)
            {
                session.Dispose();
                disposed = true;
            }
        }

        public ValueTask<bool> TryOpen(IOutboxTransaction transaction, ContextBag context, CancellationToken cancellationToken = default)
        {
            if (transaction is AzureStorageOutboxTransaction azureStorageOutboxTransaction)
            {
                session = azureStorageOutboxTransaction.StorageSession;
                session.CurrentContextBag = context;
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
            session = new StorageSession(tableHolderResolver, contextBag);
            return Task.CompletedTask;
        }

        public Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            if (ownsTransaction)
            {
                return session.Commit(cancellationToken);
            }
            return Task.CompletedTask;
        }

        readonly TableHolderResolver tableHolderResolver;
        bool disposed;
        StorageSession session;
        bool ownsTransaction;
        public CloudTable Table => session.Table;
        public TableBatchOperation Batch => session.Batch;
        public string PartitionKey => session.PartitionKey;
        public ContextBag CurrentContextBag
        {
            get => session.CurrentContextBag;
            set => session.CurrentContextBag = value;
        }

        public void Add(Operation operation) => session.Add(operation);
    }
}