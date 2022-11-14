namespace NServiceBus.Persistence.AzureTable
{
    using Azure.Data.Tables;

    sealed class TableServiceServiceClientForSubscriptionsFromConfiguration : IProvideTableServiceClientForSubscriptions
    {
        public TableServiceServiceClientForSubscriptionsFromConfiguration(TableServiceClient client)
            => Client = client;

        public TableServiceClient Client { get; }
    }
}