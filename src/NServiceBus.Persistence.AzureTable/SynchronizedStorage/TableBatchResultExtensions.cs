namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    static class TableBatchResultExtensions
    {
        internal static async Task ExecuteOperationAsync(this TableBatchOperation transactionalBatch, Operation operation, CancellationToken cancellationToken = default)
        {
            var table = operation.Apply(transactionalBatch);
            try
            {
                var batchResult = await table.ExecuteBatchAsync(transactionalBatch, cancellationToken).ConfigureAwait(false);

                if (batchResult.Count > 1)
                {
                    throw new Exception($"The transactional batch was expected to have a single operation but contained {batchResult.Count} operations.");
                }

                var result = batchResult[0];

                if (result.IsSuccessStatusCode())
                {
                    operation.Success(result);
                    return;
                }

                // guaranteed to throw
                operation.Conflict(result);
            }
            catch (StorageException e)
            {
                if (!operation.Handle(e))
                {
                    throw;
                }
            }
        }

        internal static async Task ExecuteOperationsAsync(this TableBatchOperation transactionalBatch, Dictionary<int, Operation> operationMappings, CancellationToken cancellationToken = default)
        {
            CloudTable previousTable, currentTable = null;
            foreach (var operation in operationMappings.Values)
            {
                previousTable = currentTable;
                currentTable = operation.Apply(transactionalBatch);

                var nameMatch = previousTable?.Name.Equals(currentTable?.Name, StringComparison.OrdinalIgnoreCase);
                if (nameMatch.HasValue && !nameMatch.Value)
                {
                    throw new Exception($"All operations in the same batch must be executed against the same table and the table cannot be null. Tables found: '{previousTable.Name}' and '{currentTable?.Name ?? "null"}'");
                }
            }

            if (currentTable == null)
            {
                throw new Exception("Unable to determine the table to write the current batch to. Make sure to provide the necessary table information either by providing a default table or setting it with a custom pipeline behavior");
            }

            try
            {
                var batchResult = await currentTable.ExecuteBatchAsync(transactionalBatch, cancellationToken).ConfigureAwait(false);
                for (var i = 0; i < batchResult.Count; i++)
                {
                    var result = batchResult[i];

                    operationMappings.TryGetValue(i, out var operation);
                    operation ??= ThrowOnConflictOperation.Instance;
                    if (result.IsSuccessStatusCode())
                    {
                        operation.Success(result);
                        continue;
                    }

                    // guaranteed to throw
                    operation.Conflict(result);
                }
            }
            catch (StorageException e)
            {
                for (var i = 0; i < operationMappings.Count; i++)
                {
                    if (!operationMappings[i].Handle(e))
                    {
                        throw;
                    }
                }
            }
        }
    }
}