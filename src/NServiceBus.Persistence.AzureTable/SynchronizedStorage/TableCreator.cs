namespace NServiceBus.Persistence.AzureTable
{
    using Azure.Data.Tables;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using System.Threading;

    class TableCreator
    {
        readonly ConcurrentDictionary<string, bool> createdTables = new();
        readonly bool tableCreationDisabled;

        public TableCreator(bool tableCreationDisabled)
        {
            this.tableCreationDisabled = tableCreationDisabled;
        }

        public async ValueTask CreateTableIfNotExists(TableClient tableClient, CancellationToken cancellationToken = default)
        {
            if (tableCreationDisabled || createdTables.ContainsKey(tableClient.Name))
            {
                return;
            }

            await tableClient.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
            createdTables[tableClient.Name] = true;
        }
    }
}
