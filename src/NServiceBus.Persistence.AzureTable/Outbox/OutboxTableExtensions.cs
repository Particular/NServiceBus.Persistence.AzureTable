namespace NServiceBus.Persistence.AzureTable
{
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Extensibility;

    static class OutboxTableExtensions
    {
        public static async Task<OutboxRecord> ReadOutboxRecord(this CloudTable table, string messageId, TableEntityPartitionKey partitionKey, ContextBag context)
        {
            _ = context;

            var retrieveResult = await table.ExecuteAsync(TableOperation.Retrieve<OutboxRecord>(partitionKey.PartitionKey, messageId))
                .ConfigureAwait(false);

            if (retrieveResult.HttpStatusCode == (int)HttpStatusCode.NotFound || retrieveResult.Result == null)
            {
                return default;
            }

            return (OutboxRecord)retrieveResult.Result;
        }
    }
}