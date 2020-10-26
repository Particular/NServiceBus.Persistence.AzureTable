namespace NServiceBus.Persistence.AzureStorage
{
    using System;
    using System.Collections.Generic;
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
            Batch = new TableBatchOperation();
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
            Batch.Clear();
            operations.Clear();
        }

        public void Add(Operation operation)
        {
            var operationPartitionKey = operation.PartitionKey;

            if (!operations.ContainsKey(operationPartitionKey))
            {
                operations.Add(operationPartitionKey, new Dictionary<int, Operation>());
            }

            var index = operations[operationPartitionKey].Count;
            operations[operationPartitionKey].Add(index, operation);
        }

        public async Task Commit()
        {
            // in case there is nothing to do don't even bother checking the rest
            if (operations.Count == 0)
            {
                return;
            }

            var index = 0;
            foreach (var batchOfOperations in operations)
            {
                // we need to make sure to weave in user operations if any
                var transactionalBatch = index == 0 ? Batch : new TableBatchOperation();

                await transactionalBatch.ExecuteOperationsAsync(batchOfOperations.Value)
                    .ConfigureAwait(false);

                index++;
            }
        }

        public TableHolder TableHolder { get; set; }
        public ContextBag CurrentContextBag { get; set; }

        // for the user path only
        public CloudTable Table => TableHolder?.Table;

        // for the user path only
        public TableBatchOperation Batch { get; }

        // for the user path only
        public string PartitionKey => !CurrentContextBag.TryGet<TableEntityPartitionKey>(out var partitionKey) ? null : partitionKey.PartitionKey;

        readonly bool commitOnComplete;

        readonly Dictionary<TableEntityPartitionKey, Dictionary<int, Operation>> operations = new Dictionary<TableEntityPartitionKey, Dictionary<int, Operation>>();
    }
}