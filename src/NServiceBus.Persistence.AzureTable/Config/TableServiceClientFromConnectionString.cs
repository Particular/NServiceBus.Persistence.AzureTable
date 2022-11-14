namespace NServiceBus.Persistence.AzureTable
{
    using Azure.Data.Tables;

    sealed class TableServiceClientFromConnectionString : IProvideTableServiceClient
    {
        public TableServiceClientFromConnectionString(string sagaConnectionString)
            => Client = new TableServiceClient(sagaConnectionString);

        public TableServiceClient Client { get; }
    }
}