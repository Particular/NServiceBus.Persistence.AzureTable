namespace NServiceBus
{
    using System;
    using Persistence.AzureTable;

    /// <summary>
    /// Represents the partition key when the partition key is provided at runtime through the pipeline.
    /// </summary>
    public readonly struct TableEntityPartitionKey : IEquatable<TableEntityPartitionKey>
    {
        /// <summary>
        /// Initializes the partition key information with the specified partition key value.
        /// </summary>
        /// <param name="partitionKey">The partition key value.</param>
        public TableEntityPartitionKey(string partitionKey)
        {
            Guard.AgainstNullAndEmpty(nameof(partitionKey), partitionKey);

            PartitionKey = partitionKey;
        }

        /// <inheritdoc />
        public bool Equals(TableEntityPartitionKey other)
            => string.Equals(PartitionKey, other.PartitionKey, StringComparison.OrdinalIgnoreCase);

        /// <inheritdoc />
        public override bool Equals(object obj)
            => obj is TableEntityPartitionKey other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
            => StringComparer.OrdinalIgnoreCase.GetHashCode(PartitionKey);

        /// <summary>
        /// Overloaded == equality operator
        /// </summary>
        public static bool operator ==(TableEntityPartitionKey left, TableEntityPartitionKey right)
            => left.Equals(right);

        /// <summary>
        /// Overloaded != equality operator
        /// </summary>
        public static bool operator !=(TableEntityPartitionKey left, TableEntityPartitionKey right)
            => !left.Equals(right);

        /// <summary>
        /// The partition key.
        /// </summary>
        public string PartitionKey { get; }
    }
}