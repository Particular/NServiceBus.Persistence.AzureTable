namespace NServiceBus.Persistence.AzureTable.Release_2x
{
    using System;
    using System.Runtime.Serialization;
    using Azure;
    using Azure.Data.Tables;

    public abstract class SagaDataTableEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }

        /// <summary>
        /// Yes the property name is weird but required to mimic the previous format
        /// </summary>
        /// <remarks>This property should never be manually set. It will be set automatically set.</remarks>
        public string NServiceBus_2ndIndexKey { get; set; }
    }
}