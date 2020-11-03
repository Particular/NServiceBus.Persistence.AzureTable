namespace NServiceBus.Persistence.AzureStorage
{
    using System;
    using System.Threading;

    class CurrentSharedTransactionalBatchHolder
    {
        public IWorkWithSharedTransactionalBatch Current => pipelineContext.Value.Session;

        public void SetCurrent(IWorkWithSharedTransactionalBatch session)
        {
            pipelineContext.Value.Session = session;
        }

        public Scope CreateScope()
        {
            if (pipelineContext.Value != null)
            {
                throw new InvalidOperationException("Attempt to overwrite an existing session context.");
            }
            var wrapper = new Wrapper();
            pipelineContext.Value = wrapper;
            return new Scope(this);
        }

        readonly AsyncLocal<Wrapper> pipelineContext = new AsyncLocal<Wrapper>();

        class Wrapper
        {
            public IWorkWithSharedTransactionalBatch Session;
        }

        public readonly struct Scope : IDisposable
        {
            public Scope(CurrentSharedTransactionalBatchHolder sharedTransactionalBatchHolder)
            {
                this.sharedTransactionalBatchHolder = sharedTransactionalBatchHolder;
            }

            public void Dispose()
            {
                sharedTransactionalBatchHolder.pipelineContext.Value = null;
            }

            readonly CurrentSharedTransactionalBatchHolder sharedTransactionalBatchHolder;
        }
    }
}