using System;
using NServiceBus.Persistence.AzureStorage;

namespace NServiceBus
{
    /// <summary>
    ///
    /// </summary>
    public readonly struct TableEntityPartitionKey : IEquatable<TableEntityPartitionKey>
    {
        /// <summary>
        ///
        /// </summary>
        public TableEntityPartitionKey(string partitionKey)
        {
            Guard.AgainstNullAndEmpty(nameof(partitionKey), partitionKey);

            PartitionKey = partitionKey;
        }

        /// <summary>
        ///
        /// </summary>
        public bool Equals(TableEntityPartitionKey other)
        {
            return string.Equals(PartitionKey, other.PartitionKey, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        ///
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is TableEntityPartitionKey other && Equals(other);
        }

        /// <summary>
        ///
        /// </summary>
        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(PartitionKey);
        }

        /// <summary>
        ///
        /// </summary>
        public static bool operator ==(TableEntityPartitionKey left, TableEntityPartitionKey right)
        {
            return left.Equals(right);
        }

        /// <summary>
        ///
        /// </summary>
        public static bool operator !=(TableEntityPartitionKey left, TableEntityPartitionKey right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        ///
        /// </summary>
        public string PartitionKey { get; }
    }
}