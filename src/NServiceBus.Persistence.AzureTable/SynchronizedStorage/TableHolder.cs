namespace NServiceBus.Persistence.AzureTable
{
    using Microsoft.Azure.Cosmos.Table;

    class TableHolder
    {
        public TableHolder(CloudTable table)
        {
            Table = table;
        }

        public CloudTable Table { get; set; }
    }
}