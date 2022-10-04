namespace NServiceBus.Persistence.AzureTable
{
    using Azure.Data.Tables;

    class CloudTableClientForSubscriptionsFromConfiguration : IProvideCloudTableClientForSubscriptions
    {
        public CloudTableClientForSubscriptionsFromConfiguration(TableServiceClient client)
        {
            Client = client;
        }

        public TableServiceClient Client { get; }
    }
}