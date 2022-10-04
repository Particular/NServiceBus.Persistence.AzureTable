namespace NServiceBus.Persistence.AzureTable
{
    using Azure.Data.Tables;

    class CloudTableClientFromConnectionString : IProvideCloudTableClient
    {
        public CloudTableClientFromConnectionString(string sagaConnectionString)
        {
            Client = new TableServiceClient(sagaConnectionString);
        }

        public TableServiceClient Client { get; }
    }
}