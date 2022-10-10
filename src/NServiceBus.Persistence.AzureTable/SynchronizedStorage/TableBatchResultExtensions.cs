namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Data.Tables;

    static class TableBatchResultExtensions
    {
        internal static async Task ExecuteOperationAsync(this List<TableTransactionAction> transactionalBatch, Operation operation, CancellationToken cancellationToken = default)
        {
            var table = operation.Apply(transactionalBatch);
            try
            {
                var batchResult = await table.SubmitTransactionAsync(transactionalBatch, cancellationToken).ConfigureAwait(false);

                if (batchResult.Value.Count > 1)
                {
                    throw new Exception($"The transactional batch was expected to have a single operation but contained {batchResult.Value.Count} operations.");
                }

                var result = batchResult.Value[0];

                if (result.IsSuccessStatusCode())
                {
                    operation.Success(result);
                    return;
                }

                // guaranteed to throw
                operation.Conflict(result);
            }
            catch (RequestFailedException e)
            {
                if (!operation.Handle(e))
                {
                    throw;
                }
            }
        }

        internal static async Task ExecuteOperationsAsync(this List<TableTransactionAction> transactionalBatch, Dictionary<int, Operation> operationMappings, CancellationToken cancellationToken = default)
        {
            TableClient previousTable, currentTable = null;
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
                var batchResult = await currentTable.SubmitTransactionAsync(transactionalBatch, cancellationToken).ConfigureAwait(false);
                for (var i = 0; i < batchResult.Value.Count; i++)
                {
                    var result = batchResult.Value[i];

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
            catch (RequestFailedException e)
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