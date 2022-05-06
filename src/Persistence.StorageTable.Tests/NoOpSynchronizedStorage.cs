namespace NServiceBus.PersistenceTesting
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Outbox;
    using Persistence;
    using Transport;

    class NoOpSynchronizedStorage : ICompletableSynchronizedStorageSession
    {
        public void Dispose()
        {

        }

        public ValueTask<bool> TryOpen(IOutboxTransaction transaction, ContextBag context,
                                       CancellationToken cancellationToken = new CancellationToken()) =>
            new ValueTask<bool>(false);

        public ValueTask<bool> TryOpen(TransportTransaction transportTransaction, ContextBag context,
                                       CancellationToken cancellationToken = new CancellationToken()) =>
            new ValueTask<bool>(false);

        public Task Open(ContextBag contextBag, CancellationToken cancellationToken = new CancellationToken()) =>
            Task.CompletedTask;

        public Task CompleteAsync(CancellationToken cancellationToken = new CancellationToken()) => Task.CompletedTask;
    }
}