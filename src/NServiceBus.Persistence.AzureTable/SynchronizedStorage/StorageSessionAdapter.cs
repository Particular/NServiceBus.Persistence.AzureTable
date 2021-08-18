namespace NServiceBus.Persistence.AzureTable
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Outbox;
    using Transport;

    class StorageSessionAdapter : ISynchronizedStorageAdapter
    {
        public StorageSessionAdapter(CurrentSharedTransactionalBatchHolder currentSharedTransactionalBatchHolder)
        {
            this.currentSharedTransactionalBatchHolder = currentSharedTransactionalBatchHolder;
        }

        public Task<ICompletableSynchronizedStorageSession> TryAdapt(IOutboxTransaction transaction, ContextBag context, CancellationToken cancellationToken = default)
        {
            if (transaction is AzureStorageOutboxTransaction azureStorageOutboxTransaction)
            {
                azureStorageOutboxTransaction.StorageSession.CurrentContextBag = context;
                currentSharedTransactionalBatchHolder?.SetCurrent(azureStorageOutboxTransaction.StorageSession);
                return Task.FromResult((ICompletableSynchronizedStorageSession)azureStorageOutboxTransaction.StorageSession);
            }
            return emptyResult;
        }

        public Task<ICompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context, CancellationToken cancellationToken = default)
        {
            return emptyResult;
        }

        static readonly Task<ICompletableSynchronizedStorageSession> emptyResult = Task.FromResult((ICompletableSynchronizedStorageSession)null);
        readonly CurrentSharedTransactionalBatchHolder currentSharedTransactionalBatchHolder;
    }
}