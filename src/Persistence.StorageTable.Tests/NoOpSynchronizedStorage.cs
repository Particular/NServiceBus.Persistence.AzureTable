namespace NServiceBus.PersistenceTesting
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Outbox;
    using Persistence;
    using Transport;

    class NoOpSynchronizedStorage : ISynchronizedStorage
    {
        public Task<ICompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag, CancellationToken cancellationToken = default)
        {
            return NoOpSynchronizedStorageAdapter.EmptyResult;
        }
    }

    class NoOpSynchronizedStorageAdapter : ISynchronizedStorageAdapter
    {
        public Task<ICompletableSynchronizedStorageSession> TryAdapt(IOutboxTransaction transaction, ContextBag context, CancellationToken cancellationToken = default)
        {
            return EmptyResult;
        }

        public Task<ICompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context, CancellationToken cancellationToken = default)
        {
            return EmptyResult;
        }

        internal static readonly Task<ICompletableSynchronizedStorageSession> EmptyResult = Task.FromResult<ICompletableSynchronizedStorageSession>(new NoOpCompletableSynchronizedStorageSession());
    }

    class NoOpCompletableSynchronizedStorageSession : ICompletableSynchronizedStorageSession
    {
        public Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}