namespace NServiceBus.Persistence.AzureTable
{
    using System.Threading.Tasks;
    using Extensibility;
    using Outbox;
    using Transport;

    class StorageSessionAdapter : ISynchronizedStorageAdapter
    {
        public Task<CompletableSynchronizedStorageSession> TryAdapt(OutboxTransaction transaction, ContextBag context)
        {
            if (transaction is AzureStorageOutboxTransaction azureStorageOutboxTransaction)
            {
                azureStorageOutboxTransaction.StorageSession.CurrentContextBag = context;
                return Task.FromResult((CompletableSynchronizedStorageSession)azureStorageOutboxTransaction.StorageSession);
            }
            return emptyResult;
        }

        public Task<CompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context)
        {
            return emptyResult;
        }

        static readonly Task<CompletableSynchronizedStorageSession> emptyResult = Task.FromResult((CompletableSynchronizedStorageSession)null);
    }
}