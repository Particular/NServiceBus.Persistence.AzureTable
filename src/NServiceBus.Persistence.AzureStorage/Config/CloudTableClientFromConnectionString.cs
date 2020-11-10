namespace NServiceBus.Persistence.AzureStorage
{
    using Microsoft.Azure.Cosmos.Table;

    class CloudTableClientFromConnectionString : IProvideCloudTableClient
    {
        public CloudTableClientFromConnectionString(string sagaConnectionString)
        {
            var sagaAccount = CloudStorageAccount.Parse(sagaConnectionString);
            Client = sagaAccount.CreateCloudTableClient();
        }

        public CloudTableClient Client { get; }
    }
}