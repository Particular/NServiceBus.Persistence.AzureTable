namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using Azure;
    using Azure.Data.Tables;

    class Subscription : ITableEntity
    {
        public string EndpointName { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != typeof(Subscription))
            {
                return false;
            }

            return Equals((Subscription)obj);
        }

        public virtual bool Equals(Subscription other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Equals(other.RowKey, RowKey) && Equals(other.PartitionKey, PartitionKey);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((RowKey != null ? RowKey.GetHashCode() : 0) * 397) ^ (PartitionKey != null ? PartitionKey.GetHashCode() : 0);
            }
        }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}