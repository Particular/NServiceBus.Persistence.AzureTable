namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Subscriptions
{
    using System;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using Unicast.Subscriptions;

    public class SubscriptionTestHelper
    {
        internal static AzureSubscriptionStorage CreateAzureSubscriptionStorage()
        {
            var connectionString = AzurePersistenceTests.GetConnectionString();
            var account = CloudStorageAccount.Parse(connectionString);

            var table = account.CreateCloudTableClient().GetTableReference(AzureSubscriptionStorageDefaults.TableName);
            table.CreateIfNotExists();

            return new AzureSubscriptionStorage(
                AzureSubscriptionStorageDefaults.TableName,
                connectionString,
                null);
        }

        internal static void PerformStorageCleanup()
        {
            RemoveAllRowsForTable(AzureSubscriptionStorageDefaults.TableName);
        }

        static void RemoveAllRowsForTable(string tableName)
        {
            var cloudStorageAccount = CloudStorageAccount.Parse(AzurePersistenceTests.GetConnectionString());
            var table = cloudStorageAccount.CreateCloudTableClient().GetTableReference(tableName);

            table.CreateIfNotExists();

            var projectionQuery = new TableQuery<DynamicTableEntity>().Select(new[]
            {
                "Destination"
            });

            // Define an entity resolver to work with the entity after retrieval.
            EntityResolver<Tuple<string, string>> resolver = (pk, rk, ts, props, etag) => props.ContainsKey("Destination") ? new Tuple<string, string>(pk, rk) : null;

            foreach (var tuple in table.ExecuteQuery(projectionQuery, resolver))
            {
                var tableEntity = new DynamicTableEntity(tuple.Item1, tuple.Item2)
                {
                    ETag = "*"
                };

                try
                {
                    table.Execute(TableOperation.Delete(tableEntity));
                }
                catch (StorageException)
                {
                }
            }
        }
    }
}