namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using Azure.Data.Tables;

    sealed class TableClientHolder
    {
        public TableClientHolder(TableClient tableClient) => TableClient = tableClient ?? throw new ArgumentNullException(nameof(tableClient));

        public TableClient TableClient { get; set; }
    }
}