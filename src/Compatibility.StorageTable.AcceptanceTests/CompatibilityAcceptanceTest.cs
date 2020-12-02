namespace NServiceBus.AcceptanceTests
{
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Persistence.AzureTable.Release_2x;
    using Testing;

    public class CompatibilityAcceptanceTest : NServiceBusAcceptanceTest
    {
        public CompatibilityAcceptanceTest()
        {
            PersisterUsingSecondaryIndexes = new SagaPersisterUsingSecondaryIndexes(this.GetEnvConfiguredConnectionStringByCallerConvention(), true, assumeSecondaryIndicesExist: true);
        }

        protected SagaPersisterUsingSecondaryIndexes PersisterUsingSecondaryIndexes { get; }

        protected static async Task ReplaceEntity<TSagaData>(DynamicTableEntity entity)
        {
            var table = SetupFixture.TableClient.GetTableReference($"{SetupFixture.TablePrefix}{typeof(TSagaData).Name}");

            try
            {
                await table.ExecuteAsync(TableOperation.InsertOrReplace(entity));
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    return;
                }

                throw;
            }
        }

        protected static async Task DeleteEntity<TSagaData>(DynamicTableEntity entity)
        {
            var table = SetupFixture.TableClient.GetTableReference($"{SetupFixture.TablePrefix}{typeof(TSagaData).Name}");

            try
            {
                await table.ExecuteAsync(TableOperation.Delete(entity));
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    return;
                }

                throw;
            }
        }

        protected static DynamicTableEntity GetByPartitionKey<TSagaData>(string partitionKey)
        {
            var table = SetupFixture.TableClient.GetTableReference($"{SetupFixture.TablePrefix}{typeof(TSagaData).Name}");

            // table scan but still probably the easiest way to do it, otherwise we would have to take the partition key into account which complicates things because this test is shared
            var query = new TableQuery<DynamicTableEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));

            try
            {
                var tableEntity = table.ExecuteQuery(query).FirstOrDefault();
                return tableEntity;
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    return null;
                }

                throw;
            }
        }

        protected static DynamicTableEntity GetByRowKey<TSagaData>(string rowKey)
        {
            var table = SetupFixture.TableClient.GetTableReference($"{SetupFixture.TablePrefix}{typeof(TSagaData).Name}");

            // table scan but still probably the easiest way to do it, otherwise we would have to take the partition key into account which complicates things because this test is shared
            var query = new TableQuery<DynamicTableEntity>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rowKey));

            try
            {
                var tableEntity = table.ExecuteQuery(query).FirstOrDefault();
                return tableEntity;
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    return null;
                }

                throw;
            }
        }
    }
}