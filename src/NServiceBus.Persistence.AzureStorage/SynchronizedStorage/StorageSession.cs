namespace NServiceBus.Persistence.AzureStorage
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using Microsoft.Azure.Cosmos.Table;

    class StorageSession : CompletableSynchronizedStorageSession, IWorkWithSharedTransactionalBatch
    {
        public StorageSession(TableHolderResolver resolver, ContextBag context, bool commitOnComplete)
        {
            this.commitOnComplete = commitOnComplete;
            CurrentContextBag = context;
            TableHolder = resolver.ResolveAndSetIfAvailable(context);
        }

        Task CompletableSynchronizedStorageSession.CompleteAsync()
        {
            return commitOnComplete ? Commit() : Task.CompletedTask;
        }

        void IDisposable.Dispose()
        {
            if (!commitOnComplete)
            {
                return;
            }

            Dispose();
        }

        public void Dispose()
        {
            // TODO, see if needed
            Batch.Clear();
        }

        public async Task Commit()
        {
            if (TableHolder == null)
            {
                throw new Exception("TODO");
            }

            // TODO inspect the result and act accordingly
            await Table.ExecuteBatchAsync(Batch).ConfigureAwait(false);
        }

        public TableHolder TableHolder { get; set; }
        public ContextBag CurrentContextBag { get; set; }
        public CloudTable Table => TableHolder.Table;
        public TableBatchOperation Batch { get; }

        readonly bool commitOnComplete;
    }
}