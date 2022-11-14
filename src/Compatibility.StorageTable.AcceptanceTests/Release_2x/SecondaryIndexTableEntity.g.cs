namespace NServiceBus.Persistence.AzureTable.Release_2x
{
    using System;
    using Azure;
    using Azure.Data.Tables;

    /// <summary>
    /// This mimics the secondary index table entity of the 2.4.x versions
    /// </summary>
    sealed class SecondaryIndexTableEntity : ITableEntity
    {
        public Guid SagaId { get; set; }
        public byte[] InitialSagaData { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}