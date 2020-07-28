namespace NServiceBus.PersistenceTesting
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Outbox;
    using NServiceBus.Sagas;
    using Persistence;

    public partial class PersistenceTestsConfiguration
    {
        public bool SupportsDtc { get; }
        public bool SupportsOutbox { get; }
        public bool SupportsFinders { get; }
        public bool SupportsSubscriptions { get; }
        public bool SupportsTimeouts { get; }
        public bool SupportsPessimisticConcurrency { get; }
        public ISagaIdGenerator SagaIdGenerator { get; }
        public ISagaPersister SagaStorage { get; }
        public ISynchronizedStorage SynchronizedStorage { get; }
        public ISynchronizedStorageAdapter SynchronizedStorageAdapter { get; }
        public IOutboxStorage OutboxStorage { get; }
        public Task Configure()
        {
            throw new NotImplementedException();
        }

        public Task Cleanup()
        {
            throw new NotImplementedException();
        }
    }
}