namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Microsoft.Azure.Cosmos.Table;
    using Sagas;

    class SecondaryIndex
    {
        public SecondaryIndex(bool assumeSecondaryIndicesExist = false, bool assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey = false)
        {
            this.assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey = assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey;
            this.assumeSecondaryIndicesExist = assumeSecondaryIndicesExist;
        }

        public virtual async Task<Guid?> FindSagaId<TSagaData>(CloudTable table, SagaCorrelationProperty correlationProperty, CancellationToken cancellationToken = default)
            where TSagaData : IContainSagaData
        {
            var sagaType = typeof(TSagaData);
            var key = SecondaryIndexKeyBuilder.BuildTableKey<TSagaData>(correlationProperty);

            if (cache.TryGet(key, out var guid))
            {
                return guid;
            }

            var rowKey = assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey ? key.PartitionKey : key.RowKey;

            TableResult exec;
            try
            {
                exec = await table.ExecuteAsync(TableOperation.Retrieve<SecondaryIndexTableEntity>(key.PartitionKey, rowKey), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (StorageException storageException)
                when (!assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey && storageException.RequestInformation.HttpStatusCode == (int)HttpStatusCode.PreconditionFailed)
            {
                Logger.Warn(
                    $"Trying to retrieve the secondary index entry with PartitionKey = '{key.PartitionKey}' and RowKey = 'string.Empty' failed. When using the compatibility mode on Azure Cosmos DB it is strongly recommended to enable `sagaPersistence.AssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey()` to avoid additional lookup costs or disable the compatibility mode entirely if not needed by calling `persistence.Compatibility().DisableSecondaryKeyLookupForSagasCorrelatedByProperties(). Falling back to query secondary index entry with PartitionKey = '{key.PartitionKey}' and RowKey = '{key.PartitionKey}'",
                    storageException);

                exec = await table.ExecuteAsync(TableOperation.Retrieve<SecondaryIndexTableEntity>(key.PartitionKey, key.PartitionKey), cancellationToken)
                    .ConfigureAwait(false);
            }
            if (exec.Result is SecondaryIndexTableEntity secondaryIndexEntry)
            {
                cache.Put(key, secondaryIndexEntry.SagaId);
                return secondaryIndexEntry.SagaId;
            }

            if (assumeSecondaryIndicesExist)
            {
                return null;
            }

            var ids = await ScanForSaga<TSagaData>(table, correlationProperty, cancellationToken)
                .ConfigureAwait(false);
            if (ids == null || ids.Length == 0)
            {
                return null;
            }

            if (ids.Length > 1)
            {
                throw new DuplicatedSagaFoundException(sagaType, correlationProperty.Name, ids);
            }

            // no longer creation secondary index entries
            var id = ids[0];
            cache.Put(key, id);
            return id;
        }

        static async Task<Guid[]> ScanForSaga<TSagaData>(CloudTable table, SagaCorrelationProperty correlationProperty, CancellationToken cancellationToken)
            where TSagaData : IContainSagaData
        {
            var query = DictionaryTableEntityExtensions.BuildWherePropertyQuery<TSagaData>(correlationProperty);
            query.SelectColumns = new List<string>
            {
                "PartitionKey",
                "RowKey"
            };

            var entities = await table.ExecuteQueryAsync(query, cancellationToken: cancellationToken).ConfigureAwait(false);
            return entities.Select(entity => Guid.ParseExact(entity.PartitionKey, "D")).ToArray();
        }

        public void InvalidateCache(PartitionRowKeyTuple secondaryIndexKey)
        {
            cache.Remove(secondaryIndexKey);
        }

        /// <summary>
        /// Invalidates the secondary index cache if any exists for the specified property value.
        /// </summary>
        public void InvalidateCacheIfAny<TSagaData>(SagaCorrelationProperty sagaCorrelationProperty)
            where TSagaData : IContainSagaData
        {
            var key = SecondaryIndexKeyBuilder.BuildTableKey<TSagaData>(sagaCorrelationProperty);
            cache.Remove(key);
        }

        LRUCache<PartitionRowKeyTuple, Guid> cache = new LRUCache<PartitionRowKeyTuple, Guid>(LRUCapacity);
        readonly bool assumeSecondaryIndicesExist;
        readonly bool assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey;
        const int LRUCapacity = 1000;
        static readonly ILog Logger = LogManager.GetLogger<SecondaryIndex>();
    }
}