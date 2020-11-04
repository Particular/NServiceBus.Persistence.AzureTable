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

        public async Task<Guid?> FindSagaId<TSagaData>(CloudTable table, string propertyName,
            object propertyValue)
            where TSagaData : IContainSagaData
        {
            var sagaType = typeof(TSagaData);
            var key = TryBuildKey(propertyName, propertyValue, sagaType);

            if (key == null)
            {
                return null;
            }

            if (cache.TryGet(key.Value, out var guid))
            {
                return guid;
            }

            var rowKey = assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey ? key.Value.PartitionKey : key.Value.RowKey;
            var exec = await table.ExecuteAsync(TableOperation.Retrieve<SecondaryIndexTableEntity>(key.Value.PartitionKey, rowKey))
                .ConfigureAwait(false);
            if (exec.Result is SecondaryIndexTableEntity secondaryIndexEntry)
            {
                cache.Put(key.Value, secondaryIndexEntry.SagaId);
                return secondaryIndexEntry.SagaId;
            }

            if (assumeSecondaryIndicesExist)
            {
                return null;
            }

            var ids = await ScanForSaga(table, sagaType, propertyName, propertyValue)
                .ConfigureAwait(false);
            if (ids == null || ids.Length == 0)
            {
                return null;
            }

            if (ids.Length > 1)
            {
                throw new DuplicatedSagaFoundException(sagaType, propertyName, ids);
            }

            // no longer creation secondary index entries
            var id = ids[0];
            cache.Put(key.Value, id);
            return id;
        }

        static async Task<Guid[]> ScanForSaga(CloudTable table, Type sagaType, string propertyName, object propertyValue)
        {
            var query = DictionaryTableEntityExtensions.BuildWherePropertyQuery(sagaType, propertyName, propertyValue);
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
        public void InvalidateCacheIfAny(string propertyName, object propertyValue, Type sagaType)
        {
            var key = TryBuildKey(propertyName, propertyValue, sagaType);
            if (key != null)
            {
                cache.Remove(key.Value);
            }
        }

        static PartitionRowKeyTuple? TryBuildKey(string propertyName, object propertyValue, Type sagaType)
        {
            if (string.IsNullOrEmpty(propertyName) || propertyValue == null)
            {
                return null;
            }
            return SecondaryIndexKeyBuilder.BuildTableKey(sagaType, new SagaCorrelationProperty(propertyName, propertyValue));
        }

        LRUCache<PartitionRowKeyTuple, Guid> cache = new LRUCache<PartitionRowKeyTuple, Guid>(LRUCapacity);
        private readonly bool assumeSecondaryIndicesExist;
        private readonly bool assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey;
        const int LRUCapacity = 1000;
    }
}