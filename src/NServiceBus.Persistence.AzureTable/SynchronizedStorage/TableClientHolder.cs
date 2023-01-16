namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Data.Tables;

    sealed class TableClientHolder
    {
        static readonly ConcurrentDictionary<string, bool> createdTables = new();

        public static async ValueTask CreateTableIfNotExists(TableClient tableClient, CancellationToken cancellationToken = default)
        {
            if (createdTables.ContainsKey(tableClient.Name))
            {
                return;
            }

            await tableClient.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
            createdTables[tableClient.Name] = true;
        }

        public TableClientHolder(TableClient tableClient) => TableClient = tableClient ?? throw new ArgumentNullException(nameof(tableClient));

        public TableClient TableClient { get; set; }

        public ValueTask CreateTableIfNotExists(CancellationToken cancellationToken = default) => CreateTableIfNotExists(TableClient, cancellationToken);
    }
}