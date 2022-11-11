namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Data.Tables;
    using Logging;
    using Sagas;

    class SecondaryIndex
    {
        public SecondaryIndex(bool assumeSecondaryIndicesExist = false, bool assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey = false)
        {
            this.assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey = assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey;
            this.assumeSecondaryIndicesExist = assumeSecondaryIndicesExist;
        }

        public virtual async Task<Guid?> FindSagaId<TSagaData>(TableClient table, SagaCorrelationProperty correlationProperty, CancellationToken cancellationToken = default)
            where TSagaData : IContainSagaData
        {
            var key = SecondaryIndexKeyBuilder.BuildTableKey<TSagaData>(correlationProperty);

            if (cache.TryGet(key, out var guid))
            {
                return guid;
            }

            var rowKey = assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey ? key.PartitionKey : key.RowKey;
            Response<SecondaryIndexTableEntity> exec = null;
            try
            {
                exec = await table
                    .GetEntityAsync<SecondaryIndexTableEntity>(key.PartitionKey, rowKey, null, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (RequestFailedException requestFailedException)
                when (requestFailedException.Status is (int)HttpStatusCode.NotFound)
            {
                // intentionally ignored
            }
            catch (RequestFailedException requestFailedException)
                when (!assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey && requestFailedException.Status is (int)HttpStatusCode.PreconditionFailed)
            {
                Logger.Warn(
                    $"Trying to retrieve the secondary index entry with PartitionKey = '{key.PartitionKey}' and RowKey = 'string.Empty' failed. When using the compatibility mode on Azure Cosmos DB it is strongly recommended to enable `sagaPersistence.AssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey()` to avoid additional lookup costs or disable the compatibility mode entirely if not needed by calling `persistence.Compatibility().DisableSecondaryKeyLookupForSagasCorrelatedByProperties(). Falling back to query secondary index entry with PartitionKey = '{key.PartitionKey}' and RowKey = '{key.PartitionKey}'",
                    requestFailedException);

                try
                {
                    exec = await table.GetEntityAsync<SecondaryIndexTableEntity>(key.PartitionKey, key.PartitionKey, null, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (RequestFailedException e) when (e.Status == (int)HttpStatusCode.NotFound)
                {
                    // intentionally ignored
                }
            }

            if (exec?.Value is { } secondaryIndexEntry)
            {
                cache.Put(key, secondaryIndexEntry.SagaId);
                return secondaryIndexEntry.SagaId;
            }

            if (assumeSecondaryIndicesExist)
            {
                return null;
            }

            var foundSagaIdOrNull = await ScanForSaga<TSagaData>(table, correlationProperty, cancellationToken).ConfigureAwait(false);
            if (!foundSagaIdOrNull.HasValue)
            {
                return null;
            }
            cache.Put(key, foundSagaIdOrNull.Value);
            return foundSagaIdOrNull.Value;
        }

        static async Task<Guid?> ScanForSaga<TSagaData>(TableClient table, SagaCorrelationProperty correlationProperty, CancellationToken cancellationToken)
            where TSagaData : IContainSagaData
        {
            var query = TableEntityExtensions.BuildWherePropertyQuery<TSagaData>(correlationProperty);

            var result = await table.QueryAsync<TableEntity>(query, select: SelectedColumnsForFullTableScan, cancellationToken: cancellationToken)
                                                 .ToListAsync(cancellationToken)
                                                 .ConfigureAwait(false);
            return result.Count switch
            {
                0 => null,
                1 => Guid.ParseExact(result[0].PartitionKey, "D"),
                > 1 =>
                    // Only paying the price for LINQ and list allocations in the exception case
                    throw new DuplicatedSagaFoundException(typeof(TSagaData), correlationProperty.Name,
                        result.Select(entity => Guid.ParseExact(entity.PartitionKey, "D")).ToArray()),
                _ => throw new ArgumentException() // in .NET 7 this can be switched to an UnreachableException
            };
        }

        public void InvalidateCache(PartitionRowKeyTuple secondaryIndexKey)
            => cache.Remove(secondaryIndexKey);

        /// <summary>
        /// Invalidates the secondary index cache if any exists for the specified property value.
        /// </summary>
        public void InvalidateCacheIfAny<TSagaData>(SagaCorrelationProperty sagaCorrelationProperty)
            where TSagaData : IContainSagaData
        {
            var key = SecondaryIndexKeyBuilder.BuildTableKey<TSagaData>(sagaCorrelationProperty);
            cache.Remove(key);
        }

        readonly LRUCache<PartitionRowKeyTuple, Guid> cache = new LRUCache<PartitionRowKeyTuple, Guid>(LRUCapacity);
        readonly bool assumeSecondaryIndicesExist;
        readonly bool assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey;
        const int LRUCapacity = 1000;
        static IEnumerable<string> SelectedColumnsForFullTableScan = new List<string>(2)
        {
            "PartitionKey",
            "RowKey"
        };
        static readonly ILog Logger = LogManager.GetLogger<SecondaryIndex>();
    }
}