namespace NServiceBus.Persistence.AzureStorage
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Sagas;

    class SecondaryIndex
    {
        public delegate Task<Guid[]> ScanForSagas(Type sagaType, string propertyName, object propertyValue);

        public SecondaryIndex(Func<Type, Task<CloudTable>> getTableForSaga, ScanForSagas scanner, bool assumeSecondaryIndicesExist)
        {
            this.getTableForSaga = getTableForSaga;
            this.scanner = scanner;
            this.assumeSecondaryIndicesExist = assumeSecondaryIndicesExist;
        }

        public async Task<Guid?> FindSagaId<TSagaData>(string propertyName, object propertyValue)
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

            var table = await getTableForSaga(sagaType).ConfigureAwait(false);
            var exec = await table.ExecuteAsync(TableOperation.Retrieve<SecondaryIndexTableEntity>(key.Value.PartitionKey, key.Value.RowKey))
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

            var ids = await scanner(sagaType, propertyName, propertyValue)
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

        public async Task RemoveSecondary(Type sagaType, PartitionRowKeyTuple secondaryIndexKey)
        {
            var table = await getTableForSaga(sagaType).ConfigureAwait(false);
            var e = new TableEntity
            {
                ETag = "*"
            };

            secondaryIndexKey.Apply(e);
            cache.Remove(secondaryIndexKey);
            await table.DeleteIgnoringNotFound(e).ConfigureAwait(false);
        }

        LRUCache<PartitionRowKeyTuple, Guid> cache = new LRUCache<PartitionRowKeyTuple, Guid>(LRUCapacity);
        Func<Type, Task<CloudTable>> getTableForSaga;
        bool assumeSecondaryIndicesExist;
        ScanForSagas scanner;

        const int LRUCapacity = 1000;
    }
}