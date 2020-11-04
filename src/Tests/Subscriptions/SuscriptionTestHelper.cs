namespace NServiceBus.Persistence.AzureTable.ComponentTests.Subscriptions
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Testing;

    public class SubscriptionTestHelper
    {
        internal static async Task<AzureSubscriptionStorage> CreateAzureSubscriptionStorage(string tableApiType)
        {
            var connectionString = ConnectionStringHelper.GetEnvConfiguredConnectionStringForPersistence(tableApiType);
            var account = CloudStorageAccount.Parse(connectionString);

            var table = account.CreateCloudTableClient().GetTableReference(AzureSubscriptionStorageDefaults.TableName);
            await table.CreateIfNotExistsAsync();

            return new AzureSubscriptionStorage(
                new CloudTableClientForSubscriptionsFromConnectionString(connectionString),
                AzureSubscriptionStorageDefaults.TableName,
                TimeSpan.FromSeconds(10));
        }

        internal static async Task PerformStorageCleanup(string tableApiType)
        {
            await RemoveAllRowsForTable(AzureSubscriptionStorageDefaults.TableName, tableApiType);
        }

        static async Task RemoveAllRowsForTable(string tableName, string tableApiType)
        {
            var cloudStorageAccount = CloudStorageAccount.Parse(ConnectionStringHelper.GetEnvConfiguredConnectionStringForPersistence(tableApiType));
            var table = cloudStorageAccount.CreateCloudTableClient().GetTableReference(tableName);

            await table.CreateIfNotExistsAsync();

            var projectionQuery = new TableQuery<DynamicTableEntity>().Select(new[]
            {
                "Destination"
            });

            // Define an entity resolver to work with the entity after retrieval.
            EntityResolver<Tuple<string, string>> resolver = (pk, rk, ts, props, etag) => props.ContainsKey("Destination") ? new Tuple<string, string>(pk, rk) : null;

            foreach (var tuple in await table.ExecuteQuerySegmentedAsync(
                query: projectionQuery,
                resolver: resolver,
                token: null))
            {
                var tableEntity = new DynamicTableEntity(tuple.Item1, tuple.Item2)
                {
                    ETag = "*"
                };

                try
                {
                    await table.ExecuteAsync(TableOperation.Delete(tableEntity));
                }
                catch (StorageException)
                {
                }
            }
        }
    }
}