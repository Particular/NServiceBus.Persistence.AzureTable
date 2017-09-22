namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Subscriptions
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using Unicast.Subscriptions;

    public class SubscriptionTestHelper
    {
        internal static async Task<AzureSubscriptionStorage> CreateAzureSubscriptionStorage()
        {
            var connectionString = Testing.Utillities.GetEnvConfiguredConnectionString();
            var account = CloudStorageAccount.Parse(connectionString);

            var table = account.CreateCloudTableClient().GetTableReference(AzureSubscriptionStorageDefaults.TableName);
            await table.CreateIfNotExistsAsync();

            return new AzureSubscriptionStorage(
                AzureSubscriptionStorageDefaults.TableName,
                connectionString,
                TimeSpan.FromSeconds(10));
        }

        internal static async Task PerformStorageCleanup()
        {
            await RemoveAllRowsForTable(AzureSubscriptionStorageDefaults.TableName);
        }

        static async Task RemoveAllRowsForTable(string tableName)
        {
            var cloudStorageAccount = CloudStorageAccount.Parse(Testing.Utillities.GetEnvConfiguredConnectionString());
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