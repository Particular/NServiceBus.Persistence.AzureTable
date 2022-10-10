namespace NServiceBus.Persistence.AzureTable
{
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Data.Tables;
    using Extensibility;

    static class OutboxTableExtensions
    {
        public static async Task<OutboxRecord> ReadOutboxRecord(this TableClient table, string messageId, TableEntityPartitionKey partitionKey, ContextBag context, CancellationToken cancellationToken = default)
        {
            _ = context;

            try
            {
                var retrieveResult = await table.GetEntityAsync<OutboxRecord>(partitionKey.PartitionKey, messageId, null, cancellationToken)
                                                .ConfigureAwait(false);
                return retrieveResult.Value;
            }
            catch (RequestFailedException e) when (e.Status == (int)HttpStatusCode.NotFound)
            {
                return default;
            }
        }
    }
}