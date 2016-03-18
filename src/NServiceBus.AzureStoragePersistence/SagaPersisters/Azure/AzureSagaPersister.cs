namespace NServiceBus.SagaPersisters.Azure
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Net;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using NServiceBus.Azure;
    using Extensibility;
    using Persistence;
    using Sagas;

    /// <summary>
    /// Saga persister implementation using azure table storage.
    /// </summary>
    public class AzureSagaPersister : ISagaPersister
    {
        readonly bool autoUpdateSchema;
        readonly CloudTableClient client;
        static readonly ConcurrentDictionary<string, bool> tableCreated = new ConcurrentDictionary<string, bool>();
        static readonly ConditionalWeakTable<object, string> etags = new ConditionalWeakTable<object, string>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString">The Azure storage connection string</param>
        /// <param name="autoUpdateSchema">Indicates if the storage tables should be auto created if they do not exist</param>
        public AzureSagaPersister(string connectionString, bool autoUpdateSchema)
        {
            this.autoUpdateSchema = autoUpdateSchema;
            var account = CloudStorageAccount.Parse(connectionString);
            client = account.CreateCloudTableClient();
        }

        DictionaryTableEntity GetDictionaryTableEntity(string sagaId, Type entityType)
        {
            var tableName = entityType.Name;
            var table = client.GetTableReference(tableName);

            var query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sagaId));

            var tableEntity = table.ExecuteQuery(query).SafeFirstOrDefault();
            return tableEntity;
        }

        DictionaryTableEntity GetDictionaryTableEntity(Type type, string property, object value)
        {
            var tableName = type.Name;
            var table = client.GetTableReference(tableName);

            TableQuery<DictionaryTableEntity> query;

            var propertyInfo = type.GetProperty(property);
            if (propertyInfo == null)
            {
                return null;
            }

            if (propertyInfo.PropertyType == typeof(byte[]))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForBinary(property, QueryComparisons.Equal, (byte[])value));
            }
            else if (propertyInfo.PropertyType == typeof(bool))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForBool(property, QueryComparisons.Equal, (bool)value));
            }
            else if (propertyInfo.PropertyType == typeof(DateTime))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForDate(property, QueryComparisons.Equal, (DateTime)value));
            }
            else if (propertyInfo.PropertyType == typeof(Guid))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForGuid(property, QueryComparisons.Equal, (Guid)value));
            }
            else if (propertyInfo.PropertyType == typeof(Int32))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForInt(property, QueryComparisons.Equal, (int)value));
            }
            else if (propertyInfo.PropertyType == typeof(Int64))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForLong(property, QueryComparisons.Equal, (long)value));
            }
            else if (propertyInfo.PropertyType == typeof(Double))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForDouble(property, QueryComparisons.Equal, (double)value));
            }
            else if (propertyInfo.PropertyType == typeof(string))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterCondition(property, QueryComparisons.Equal, (string)value));
            }
            else
            {
                throw new NotSupportedException(
                    string.Format("The property type '{0}' is not supported in windows azure table storage",
                        propertyInfo.PropertyType.Name));
            }

            var tableEntity = table.ExecuteQuery(query).SafeFirstOrDefault();
            return tableEntity;
        }

        async Task Persist(IContainSagaData saga)
        {
            var type = saga.GetType();
            var tableName = type.Name;
            var table = client.GetTableReference(tableName);
            if (autoUpdateSchema && !tableCreated.ContainsKey(tableName))
            {
                table.CreateIfNotExists();
                tableCreated[tableName] = true;
            }

            var partitionKey = saga.Id.ToString();

            var batch = new TableBatchOperation();

            AddObjectToBatch(batch, saga, partitionKey);

            await table.ExecuteBatchAsync(batch).ConfigureAwait(false);
        }

        void AddObjectToBatch(TableBatchOperation batch, object entity, string partitionKey, string rowkey = "")
        {
            if (rowkey == "") rowkey = partitionKey; // just to be backward compat with original implementation

            var type = entity.GetType();
            string etag;
            var update = etags.TryGetValue(entity, out etag);

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var toPersist = ToDictionaryTableEntity(entity, new DictionaryTableEntity { PartitionKey = partitionKey, RowKey = rowkey, ETag = etag }, properties);

            //no longer using InsertOrReplace as it ignores concurrency checks
            batch.Add(update ? TableOperation.Replace(toPersist) : TableOperation.Insert(toPersist));
        }

        DictionaryTableEntity ToDictionaryTableEntity(object entity, DictionaryTableEntity toPersist, IEnumerable<PropertyInfo> properties)
        {
            foreach (var propertyInfo in properties)
            {
                if (propertyInfo.PropertyType == typeof(byte[]))
                {
                    toPersist[propertyInfo.Name] = new EntityProperty((byte[])propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof(bool))
                {
                    toPersist[propertyInfo.Name] = new EntityProperty((bool)propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof(DateTime))
                {
                    toPersist[propertyInfo.Name] = new EntityProperty((DateTime)propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof(Guid))
                {
                    toPersist[propertyInfo.Name] = new EntityProperty((Guid)propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof(Int32))
                {
                    toPersist[propertyInfo.Name] = new EntityProperty((Int32)propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof(Int64))
                {
                    toPersist[propertyInfo.Name] = new EntityProperty((Int64)propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof(Double))
                {
                    toPersist[propertyInfo.Name] = new EntityProperty((Double)propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof(string))
                {
                    toPersist[propertyInfo.Name] = new EntityProperty((string)propertyInfo.GetValue(entity, null));
                }
                else
                {
                    throw new NotSupportedException(
                        string.Format("The property type '{0}' is not supported in windows azure table storage",
                                      propertyInfo.PropertyType.Name));
                }
            }
            return toPersist;
        }

        object ToEntity(Type entityType, DictionaryTableEntity entity)
        {
            if (entity == null) return null;

            var toCreate = Activator.CreateInstance(entityType);
            foreach (var propertyInfo in entityType.GetProperties())
            {
                if (entity.ContainsKey(propertyInfo.Name))
                {
                    if (propertyInfo.PropertyType == typeof(byte[]))
                    {
                        propertyInfo.SetValue(toCreate, entity[propertyInfo.Name].BinaryValue, null);
                    }
                    else if (propertyInfo.PropertyType == typeof(bool))
                    {
                        var boolean = entity[propertyInfo.Name].BooleanValue;
                        propertyInfo.SetValue(toCreate, boolean.HasValue && boolean.Value, null);
                    }
                    else if (propertyInfo.PropertyType == typeof(DateTime))
                    {
                        var dateTimeOffset = entity[propertyInfo.Name].DateTimeOffsetValue;
                        propertyInfo.SetValue(toCreate, dateTimeOffset.HasValue ? dateTimeOffset.Value.DateTime : default(DateTime), null);
                    }
                    else if (propertyInfo.PropertyType == typeof(Guid))
                    {
                        var guid = entity[propertyInfo.Name].GuidValue;
                        propertyInfo.SetValue(toCreate, guid.HasValue ? guid.Value : default(Guid), null);
                    }
                    else if (propertyInfo.PropertyType == typeof(Int32))
                    {
                        var int32 = entity[propertyInfo.Name].Int32Value;
                        propertyInfo.SetValue(toCreate, int32.HasValue ? int32.Value : default(Int32), null);
                    }
                    else if (propertyInfo.PropertyType == typeof(Double))
                    {
                        var d = entity[propertyInfo.Name].DoubleValue;
                        propertyInfo.SetValue(toCreate, d.HasValue ? d.Value : default(Int64), null);
                    }
                    else if (propertyInfo.PropertyType == typeof(Int64))
                    {
                        var int64 = entity[propertyInfo.Name].Int64Value;
                        propertyInfo.SetValue(toCreate, int64.HasValue ? int64.Value : default(Int64), null);
                    }
                    else if (propertyInfo.PropertyType == typeof(string))
                    {
                        propertyInfo.SetValue(toCreate, entity[propertyInfo.Name].StringValue, null);
                    }
                    else
                    {
                        throw new NotSupportedException(
                            string.Format("The property type '{0}' is not supported in windows azure table storage",
                                propertyInfo.PropertyType.Name));
                    }
                }
            }
            return toCreate;
        }

        /// <summary>
        /// Saves the given saga entity using the current session of the
        /// injected session factory.
        /// </summary>
        /// <param name="sagaData">The saga entity that will be saved.</param>
        /// <param name="correlationProperty">The correlation property.</param>
        /// <param name="session">The synchronization session.</param>
        /// <param name="context">The current context.</param>
        public Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
        {
            return Persist(sagaData);
        }

        /// <summary>
        /// Updates the given saga entity using the current session of the
        /// injected session factory.
        /// </summary>
        /// <param name="sagaData">The saga entity that will be updated.</param>
        /// <param name="session">The synchronization session.</param>
        /// <param name="context">The current context.</param>
        public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            return Persist(sagaData);
        }

        /// <summary>
        /// Gets a saga entity from the injected session factory's current session
        /// using the given saga id.
        /// </summary>
        /// <param name="sagaId">The saga id to use in the lookup.</param>
        /// <param name="session">The synchronization session.</param>
        /// <param name="context">The current context.</param>
        /// <returns>The saga entity if found, otherwise null.</returns>
        public Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            var id = sagaId.ToString();
            var entityType = typeof(TSagaData);
            var tableEntity = GetDictionaryTableEntity(id, entityType);
            var entity = (TSagaData)ToEntity(entityType, tableEntity);

            if (!Equals(entity, default(TSagaData)))
            {
                etags.Add(entity, tableEntity.ETag);
            }

            return Task.FromResult(entity);
        }

        public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            var type = typeof(TSagaData);
            try
            {
                var tableEntity = GetDictionaryTableEntity(type, propertyName, propertyValue);
                var entity = (TSagaData)ToEntity(type, tableEntity);

                if (!Equals(entity, default(TSagaData)))
                {
                    etags.Add(entity, tableEntity.ETag);
                }

                return Task.FromResult(entity);
            }
            catch (WebException ex)
            {
                // can occur when table has not yet been created, but already looking for absence of instance
                if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                {
                    var response = (HttpWebResponse)ex.Response;
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return Task.FromResult(default(TSagaData));
                    }
                }

                throw;
            }
            catch (StorageException)
            {
                // can occur when table has not yet been created, but already looking for absence of instance
                return Task.FromResult(default(TSagaData));
            }
        }

        /// <summary>
        /// Deletes the given saga from the injected session factory's
        /// current session.
        /// </summary>
        /// <param name="sagaData">The saga entity that will be deleted.</param>
        /// <param name="session">The storage session.</param>
        /// <param name="context">The current context.</param>
        public async Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var tableName = sagaData.GetType().Name;
            var table = client.GetTableReference(tableName);

            var query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sagaData.Id.ToString()));

            var entity = table.ExecuteQuery(query).SafeFirstOrDefault();
            if (entity == null)
            {
                return; // should not try to delete saga data that does not exist, this situation can occur on retry or parallel execution
            }

            try
            {
                await table.ExecuteAsync(TableOperation.Delete(entity)).ConfigureAwait(false);
            }
            catch (StorageException ex)
            {
                // Horrible logic to check if item has already been deleted or not
                var webException = ex.InnerException as WebException;
                if (webException?.Response != null)
                {
                    var response = (HttpWebResponse)webException.Response;
                    if ((int)response.StatusCode != 404)
                    {
                        // Was not a previously deleted exception, throw again
                        throw;
                    }
                }
            }
        }
    }
}