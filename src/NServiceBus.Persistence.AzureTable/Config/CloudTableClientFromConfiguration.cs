namespace NServiceBus.Persistence.AzureTable
{
    using Azure.Data.Tables;

    class CloudTableClientFromConfiguration : IProvideCloudTableClient
    {
        public CloudTableClientFromConfiguration(TableServiceClient client)
        {
            Client = client;
        }

        public TableServiceClient Client { get; }
    }
}