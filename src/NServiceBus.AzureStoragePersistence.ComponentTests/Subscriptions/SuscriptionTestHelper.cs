namespace NServiceBus.AzureStoragePersistence.ComponentTests.Subscriptions
{
    using System;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using NServiceBus.Config;
    using NServiceBus.Unicast.Subscriptions;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

    public class SuscriptionTestHelper
    {
        internal static ISubscriptionStorage CreateAzureSubscriptionStorage()
        {
            var config = new AzureSubscriptionStorageConfig();
            var connectionString = AzurePersistenceTests.GetConnectionString();
            var account = CloudStorageAccount.Parse(connectionString);

            var table = account.CreateCloudTableClient().GetTableReference(config.TableName);
            table.CreateIfNotExists();

            return new AzureSubscriptionStorage(
                config.TableName, 
                connectionString);
        }

        internal static void PerformStorageCleanup()
        {
            RemoveAllRowsForTable(new AzureSubscriptionStorageConfig().TableName);
        }

        static void RemoveAllRowsForTable(string tableName)
        {
            var cloudStorageAccount = CloudStorageAccount.Parse(AzurePersistenceTests.GetConnectionString());
            var table = cloudStorageAccount.CreateCloudTableClient().GetTableReference(tableName);

            table.DeleteIfExists();
            table.CreateIfNotExists();

            var projectionQuery = new TableQuery<DynamicTableEntity>().Select(new[] { "Destination" });

            // Define an entity resolver to work with the entity after retrieval.
            EntityResolver<Tuple<string, string>> resolver = (pk, rk, ts, props, etag) => props.ContainsKey("Destination") ? new Tuple<string, string>(pk, rk) : null;

            foreach (var tuple in table.ExecuteQuery(projectionQuery, resolver))
            {
                var tableEntity = new DynamicTableEntity(tuple.Item1, tuple.Item2) { ETag = "*" };
                table.Execute(TableOperation.Delete(tableEntity));
            }
        }
    }
}