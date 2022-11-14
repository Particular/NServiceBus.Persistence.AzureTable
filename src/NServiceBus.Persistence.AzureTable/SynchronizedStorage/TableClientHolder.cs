namespace NServiceBus.Persistence.AzureTable
{
    using Azure.Data.Tables;

    class TableClientHolder
    {
        public TableClientHolder(TableClient tableClient)
        {
            TableClient = tableClient;
        }

        public TableClient TableClient { get; set; }
    }
}