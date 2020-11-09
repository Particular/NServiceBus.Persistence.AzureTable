namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Sagas;

    class SecondaryIndex
    {
        public SecondaryIndex(bool assumeSecondaryIndicesExist = false, bool assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey = false)
        {
            this.assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey = assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey;
            this.assumeSecondaryIndicesExist = assumeSecondaryIndicesExist;
        }

        public virtual async Task<Guid?> FindSagaId<TSagaData>(CloudTable table, SagaCorrelationProperty correlationProperty)
            where TSagaData : IContainSagaData
        {
            var sagaType = typeof(TSagaData);
            var key = SecondaryIndexKeyBuilder.BuildTableKey<TSagaData>(correlationProperty);

            if (cache.TryGet(key, out var guid))
            {
                return guid;
            }

            var rowKey = assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey ? key.PartitionKey : key.RowKey;
            var exec = await table.ExecuteAsync(TableOperation.Retrieve<SecondaryIndexTableEntity>(key.PartitionKey, rowKey))
                .ConfigureAwait(false);
            if (exec.Result is SecondaryIndexTableEntity secondaryIndexEntry)
            {
                cache.Put(key, secondaryIndexEntry.SagaId);
                return secondaryIndexEntry.SagaId;
            }

            if (assumeSecondaryIndicesExist)
            {
                return null;
            }

            var ids = await ScanForSaga<TSagaData>(table, correlationProperty)
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

        static async Task<Guid[]> ScanForSaga<TSagaData>(CloudTable table, SagaCorrelationProperty correlationProperty)
            where TSagaData : IContainSagaData
        {
            var query = DictionaryTableEntityExtensions.BuildWherePropertyQuery<TSagaData>(correlationProperty);
            query.SelectColumns = new List<string>
            {
                "PartitionKey",
                "RowKey"
            };

            var entities = await table.ExecuteQueryAsync(query).ConfigureAwait(false);
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

        static PartitionRowKeyTuple? TryBuildKey<TSagaData>(SagaCorrelationProperty correlationProperty)
            where TSagaData : IContainSagaData
        {
            return SecondaryIndexKeyBuilder.BuildTableKey<TSagaData>(correlationProperty);
        }

        LRUCache<PartitionRowKeyTuple, Guid> cache = new LRUCache<PartitionRowKeyTuple, Guid>(LRUCapacity);
        private readonly bool assumeSecondaryIndicesExist;
        private readonly bool assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey;
        const int LRUCapacity = 1000;
    }
}