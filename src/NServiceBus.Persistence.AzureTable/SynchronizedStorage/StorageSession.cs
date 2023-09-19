namespace NServiceBus.Persistence.AzureTable
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Data.Tables;
    using Extensibility;

    class StorageSession : IWorkWithSharedTransactionalBatch
    {
        public StorageSession(TableClientHolderResolver resolver, ContextBag context)
        {
            CurrentContextBag = context;
            TableClientHolder = resolver.ResolveAndSetIfAvailable(context);
            Batch = [];
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

        public async Task Commit(CancellationToken cancellationToken = default)
        {
            foreach (var operation in Batch)
            {
                Add(new UserOperation(CurrentContextBag.Get<TableEntityPartitionKey>(), Table, operation));
            }

            // in case there is nothing to do don't even bother checking the rest
            if (operations.Count == 0)
            {
                return;
            }

            foreach (var batchOfOperations in operations)
            {
                var transactionalBatch = new List<TableTransactionAction>();
                await transactionalBatch
                      .ExecuteOperationsAsync(batchOfOperations.Value, cancellationToken: cancellationToken)
                      .ConfigureAwait(false);
            }
        }

        public TableClientHolder TableClientHolder { get; set; }
        public ContextBag CurrentContextBag { get; set; }


        // for the user path only
        public TableClient Table => TableClientHolder?.TableClient;

        // for the user path only
        public List<TableTransactionAction> Batch { get; }

        // for the user path only
        public string PartitionKey => !CurrentContextBag.TryGet<TableEntityPartitionKey>(out var partitionKey)
            ? null
            : partitionKey.PartitionKey;

        readonly Dictionary<TableEntityPartitionKey, Dictionary<int, Operation>> operations = new();
    }
}