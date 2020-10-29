namespace NServiceBus.AcceptanceTests
{
    using System.Linq;
    using System.Net;
    using Microsoft.Azure.Cosmos.Table;
    using Persistence.AzureStorage.Previous;
    using NServiceBus.Persistence.AzureStorage.Testing;

    public class MigrationAcceptanceTest : NServiceBusAcceptanceTest
    {
        public MigrationAcceptanceTest()
        {
            PersisterUsingSecondaryIndexes = new SagaPersisterUsingSecondaryIndexes(Utilities.GetEnvConfiguredConnectionStringForPersistence(), true, assumeSecondaryIndicesExist: true);
        }

        protected SagaPersisterUsingSecondaryIndexes PersisterUsingSecondaryIndexes { get; }

        protected static DynamicTableEntity GetByPartitionKey<TSagaData>(string partitionKey)
        {
            var table = SetupFixture.TableClient.GetTableReference(typeof(TSagaData).Name);

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
            var table = SetupFixture.TableClient.GetTableReference(typeof(TSagaData).Name);

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