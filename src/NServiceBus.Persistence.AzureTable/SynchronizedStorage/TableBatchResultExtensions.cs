namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;

    static class TableBatchResultExtensions
    {
        internal static async Task ExecuteOperationAsync(this TableBatchOperation transactionalBatch, Operation operation)
        {
            var table = operation.Apply(transactionalBatch);
            try
            {
                var batchResult = await table.ExecuteBatchAsync(transactionalBatch).ConfigureAwait(false);

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

        internal static async Task ExecuteOperationsAsync(this TableBatchOperation transactionalBatch, Dictionary<int, Operation> operationMappings)
        {
            CloudTable previousTable, currentTable = null;
            foreach (var operation in operationMappings.Values)
            {
                previousTable = currentTable;
                currentTable = operation.Apply(transactionalBatch);

                var nameMatch = previousTable?.Name.Equals(currentTable.Name, StringComparison.OrdinalIgnoreCase);
                if (nameMatch.HasValue && !nameMatch.Value)
                {
                    throw new Exception("Table must match for the same batch");
                }
            }

            if (currentTable == null)
            {
                throw new Exception("TODO");
            }

            try
            {
                var batchResult = await currentTable.ExecuteBatchAsync(transactionalBatch).ConfigureAwait(false);
                for (var i = 0; i < batchResult.Count; i++)
                {
                    var result = batchResult[i];

                    operationMappings.TryGetValue(i, out var operation);
                    // operation = operation ?? ThrowOnConflictOperation.Instance;
                    if (result.IsSuccessStatusCode())
                    {
                        operation.Success(result);
                        continue;
                    }

                    // TODO: Check if this even makes sense
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