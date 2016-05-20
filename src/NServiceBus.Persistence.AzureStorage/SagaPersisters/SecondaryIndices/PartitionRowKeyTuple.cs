namespace NServiceBus.Persistence.AzureStorage.SecondaryIndices
{
    using System;
    using Microsoft.WindowsAzure.Storage.Table;

    sealed class PartitionRowKeyTuple
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

        bool Equals(PartitionRowKeyTuple other)
        {
            return string.Equals(PartitionKey, other.PartitionKey) && string.Equals(RowKey, other.RowKey);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != GetType())
            {
                return false;
            }
            return Equals((PartitionRowKeyTuple) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((PartitionKey?.GetHashCode() ?? 0)*397) ^ (RowKey?.GetHashCode() ?? 0);
            }
        }

        public override string ToString()
        {
            return PartitionKey + Separator + RowKey;
        }

        public static PartitionRowKeyTuple Parse(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return null;
            }
            var strings = str.Split(Separators, StringSplitOptions.None);
            return new PartitionRowKeyTuple(strings[0], strings[1]);
        }

        const string Separator = "#";

        static readonly string[] Separators =
        {
            Separator
        };
    }
}