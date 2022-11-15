namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using Azure;
    using Azure.Data.Tables;

    // An entity holding information about the secondary index.
    sealed class SecondaryIndexTableEntity : ITableEntity
    {
        public Guid SagaId { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}