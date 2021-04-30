namespace NServiceBus.Persistence.AzureTable
{
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Microsoft.Azure.Cosmos.Table;

    static class OutboxTableExtensions
    {
        public static async Task<OutboxRecord> ReadOutboxRecord(this CloudTable table, string messageId, TableEntityPartitionKey partitionKey, ContextBag context, CancellationToken cancellationToken = default)
        {
            _ = context;

            var retrieveResult = await table.ExecuteAsync(TableOperation.Retrieve<OutboxRecord>(partitionKey.PartitionKey, messageId), cancellationToken)
                .ConfigureAwait(false);

            if (retrieveResult.HttpStatusCode == (int)HttpStatusCode.NotFound || retrieveResult.Result == null)
            {
                return default;
            }

            return (OutboxRecord)retrieveResult.Result;
        }
    }
}