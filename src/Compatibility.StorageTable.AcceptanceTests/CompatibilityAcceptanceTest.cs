namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Data.Tables;
    using Persistence.AzureTable.Release_2x;
    using Sagas;

    public abstract class CompatibilityAcceptanceTest : NServiceBusAcceptanceTest
    {
        /// <summary>
        /// Stores the saga table entity in a format that mimics the 2.4.x version of the saga persistence.
        /// </summary>
        /// <param name="sagaData">The saga data table entity that must be annotated with <see cref="SagaEntityTypeAttribute"/> that points to the corresponding saga data class.</param>
        /// <param name="correlationProperty">The correlation property.</param>
        /// <typeparam name="TSagaTableEntity">The saga table entity type.</typeparam>
        /// <exception cref="InvalidOperationException">Throw when the <typeparam name="TSagaTableEntity"/> is not annotated with <see cref="SagaEntityTypeAttribute"/></exception>
        protected static async Task SaveSagaInOldFormat<TSagaTableEntity>(TSagaTableEntity sagaData, SagaCorrelationProperty correlationProperty)
            where TSagaTableEntity : SagaDataTableEntity
        {
            var tableServiceClient = SetupFixture.TableServiceClient;

            if (typeof(TSagaTableEntity).GetCustomAttribute(typeof(SagaEntityTypeAttribute)) is not SagaEntityTypeAttribute sagaEntityTypeAttribute)
            {
                throw new InvalidOperationException($@"Specify '{nameof(SagaEntityTypeAttribute)}' on the entity type that points to the corresponding saga data class. 
For example:
public class MigratedSagaData : ContainSagaData {{ }}

requires a corresponding

[SagaEntityType(SagaEntityType = typeof(MigratedSagaData))]
public class MigratedSagaDataTableEntity : SagaDataTableEntity {{ }}
");
            }

            var sagaDataType = sagaEntityTypeAttribute.SagaEntityType;
            var tableClient = tableServiceClient.GetTableClient($"{SetupFixture.TablePrefix}{sagaDataType.Name}");

            // Given this code doesn't need to deal with race conditions like the 2.4.x version the order of operations
            // doesn't matter and the third step of remove the data of the primary from the 2nd index. is executed in one
            // operation. The original order was
            // The following operations have to be executed sequentially:
            // 1) insert the 2nd index, containing the primary saga data (just in case of a failure)
            // 2) insert the primary saga data in its row, storing the identifier of the secondary index as well (for completions)
            // 3) remove the data of the primary from the 2nd index. It will be no longer needed

            var key = SecondaryIndexKeyBuilder.BuildTableKey(sagaDataType, correlationProperty);

            var newSecondaryIndexEntity = new SecondaryIndexTableEntity
            {
                SagaId = new Guid(sagaData.RowKey),
                PartitionKey = key.PartitionKey,
                RowKey = key.RowKey,
                ETag = ETag.All
            };

            sagaData.NServiceBus_2ndIndexKey = key.ToString();

            await tableClient.UpsertEntityAsync(sagaData);
            await tableClient.UpsertEntityAsync(newSecondaryIndexEntity);
        }

        protected static async Task ReplaceEntity<TSagaData>(ITableEntity entity)
        {
            var tableServiceClient = SetupFixture.TableServiceClient;
            var tableClient = tableServiceClient.GetTableClient($"{SetupFixture.TablePrefix}{typeof(TSagaData).Name}");

            try
            {
                await tableClient.UpsertEntityAsync(entity);
            }
            catch (RequestFailedException e)
            {
                if (e.Status == (int)HttpStatusCode.NotFound)
                {
                    return;
                }

                throw;
            }
        }

        protected static async Task DeleteEntity<TSagaData>(ITableEntity entity)
        {
            var tableServiceClient = SetupFixture.TableServiceClient;
            var tableClient = tableServiceClient.GetTableClient($"{SetupFixture.TablePrefix}{typeof(TSagaData).Name}");

            try
            {
                await tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey);
            }
            catch (RequestFailedException e)
            {
                if (e.Status == (int)HttpStatusCode.NotFound)
                {
                    return;
                }

                throw;
            }
        }

        protected static async Task<TableEntity> GetByPartitionKey<TSagaData>(string partitionKey)
        {
            var tableServiceClient = SetupFixture.TableServiceClient;
            var tableClient = tableServiceClient.GetTableClient($"{SetupFixture.TablePrefix}{typeof(TSagaData).Name}");

            try
            {
                // table scan but still probably the easiest way to do it, otherwise we would have to take the partition key into account which complicates things because this test is shared
                var entities = await tableClient.QueryAsync<TableEntity>()
                    .Where(x => x.PartitionKey == partitionKey)
                    .ToListAsync();
                return entities.FirstOrDefault();
            }
            catch (RequestFailedException e)
            {
                if (e.Status == (int)HttpStatusCode.NotFound)
                {
                    return null;
                }

                throw;
            }
        }

        protected static async Task<TableEntity> GetByRowKey<TSagaData>(string rowKey)
        {
            var tableServiceClient = SetupFixture.TableServiceClient;
            var tableClient = tableServiceClient.GetTableClient($"{SetupFixture.TablePrefix}{typeof(TSagaData).Name}");

            try
            {
                // table scan but still probably the easiest way to do it, otherwise we would have to take the partition key into account which complicates things because this test is shared
                var entities = await tableClient.QueryAsync<TableEntity>()
                    .Where(x => x.RowKey == rowKey)
                    .ToListAsync();
                return entities.FirstOrDefault();
            }
            catch (RequestFailedException e)
            {
                if (e.Status == (int)HttpStatusCode.NotFound)
                {
                    return null;
                }

                throw;
            }
        }
    }
}