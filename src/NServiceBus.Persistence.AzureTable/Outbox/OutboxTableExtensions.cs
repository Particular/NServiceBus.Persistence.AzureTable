namespace NServiceBus.Persistence.AzureTable
{
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Data.Tables;
    using Extensibility;

    static class OutboxTableExtensions
    {
        public static async Task<OutboxRecord> ReadOutboxRecord(this TableClient tableClient, string messageId, TableEntityPartitionKey partitionKey, ContextBag context, CancellationToken cancellationToken = default)
        {
            _ = context;

            var retrieveResult = await tableClient.GetEntityIfExistsAsync<OutboxRecord>(partitionKey.PartitionKey, messageId, null, cancellationToken)
                .ConfigureAwait(false);
            return retrieveResult.HasValue ? retrieveResult.Value : default;
        }
    }
}