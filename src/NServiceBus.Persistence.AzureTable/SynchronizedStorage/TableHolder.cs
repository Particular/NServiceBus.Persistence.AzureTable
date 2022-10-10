namespace NServiceBus.Persistence.AzureTable
{
    using Azure.Data.Tables;

    class TableHolder
    {
        public TableHolder(TableClient table)
        {
            Table = table;
        }

        public TableClient Table { get; set; }
    }
}