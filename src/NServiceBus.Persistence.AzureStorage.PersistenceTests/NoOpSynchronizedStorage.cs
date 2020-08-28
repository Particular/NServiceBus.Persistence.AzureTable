namespace NServiceBus.PersistenceTesting
{
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Outbox;
    using Persistence;
    using Transport;

    class NoOpSynchronizedStorage : ISynchronizedStorage
    {
        public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
        {
            return NoOpSynchronizedStorageAdapter.EmptyResult;
        }
    }

    class NoOpSynchronizedStorageAdapter : ISynchronizedStorageAdapter
    {
        public Task<CompletableSynchronizedStorageSession> TryAdapt(OutboxTransaction transaction, ContextBag context)
        {
            return EmptyResult;
        }

        public Task<CompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context)
        {
            return EmptyResult;
        }

        internal static readonly Task<CompletableSynchronizedStorageSession> EmptyResult = Task.FromResult<CompletableSynchronizedStorageSession>(new NoOpCompletableSynchronizedStorageSession());
    }

    class NoOpCompletableSynchronizedStorageSession : CompletableSynchronizedStorageSession
    {
        public Task CompleteAsync()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}