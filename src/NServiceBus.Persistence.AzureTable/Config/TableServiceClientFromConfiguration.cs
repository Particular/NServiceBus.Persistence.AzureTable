namespace NServiceBus.Persistence.AzureTable
{
    using Azure.Data.Tables;

    class TableServiceClientFromConfiguration : IProvideTableServiceClient
    {
        public TableServiceClientFromConfiguration(TableServiceClient client)
        {
            Client = client;
        }

        public TableServiceClient Client { get; }
    }
}