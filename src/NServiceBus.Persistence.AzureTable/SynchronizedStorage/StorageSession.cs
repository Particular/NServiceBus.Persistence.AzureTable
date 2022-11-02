namespace NServiceBus.Persistence.AzureTable
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Data.Tables;
    using Extensibility;

    class StorageSession : IWorkWithSharedTransactionalBatch
    {
        public StorageSession(TableHolderResolver resolver, ContextBag context)
        {
            CurrentContextBag = context;
            TableHolder = resolver.ResolveAndSetIfAvailable(context);
            BatchOperations = new List<TableTransactionAction>();
        }

        public void Dispose()
        {
            BatchOperations.Clear();
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
            foreach (var operation in BatchOperations)
            {
                Add(new UserOperation(CurrentContextBag.Get<TableEntityPartitionKey>(), TableClient, operation));
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

        public TableHolder TableHolder { get; set; }
        public ContextBag CurrentContextBag { get; set; }


        // for the user path only
        public TableClient TableClient => TableHolder?.Table;

        // for the user path only
        public List<TableTransactionAction> BatchOperations { get; }

        // for the user path only
        public string PartitionKey => !CurrentContextBag.TryGet<TableEntityPartitionKey>(out var partitionKey)
            ? null
            : partitionKey.PartitionKey;

        readonly Dictionary<TableEntityPartitionKey, Dictionary<int, Operation>> operations = new();
    }
}