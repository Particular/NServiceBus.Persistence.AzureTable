namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using Azure.Data.Tables;

    struct PartitionRowKeyTuple
    {
        public PartitionRowKeyTuple(string partitionKey, string rowKey)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }

        public string PartitionKey { get; }

        public string RowKey { get; }

        public void Apply(ITableEntity entity)
        {
            entity.PartitionKey = PartitionKey;
            entity.RowKey = RowKey;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            return obj is PartitionRowKeyTuple tuple && Equals(tuple);
        }

        bool Equals(PartitionRowKeyTuple other)
        {
            return string.Equals(PartitionKey, other.PartitionKey) && string.Equals(RowKey, other.RowKey);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((PartitionKey?.GetHashCode() ?? 0) * 397) ^ (RowKey?.GetHashCode() ?? 0);
            }
        }

        public override string ToString()
        {
            return $"{PartitionKey}{Separator}{RowKey}";
        }

        public static PartitionRowKeyTuple? Parse(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return null;
            }

            var strings = str.Split(separator, StringSplitOptions.None);
            return new PartitionRowKeyTuple(strings[0], strings[1]);
        }

        static string[] separator = { Separator };
        const string Separator = "#";
    }
}