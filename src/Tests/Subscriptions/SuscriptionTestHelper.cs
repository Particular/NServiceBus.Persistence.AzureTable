namespace NServiceBus.Persistence.AzureTable.Tests
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Azure.Data.Tables;
    using Testing;

    public static class SubscriptionTestHelper
    {
        internal static async Task<Scope> CreateAzureSubscriptionStorage(string tableApiType)
        {
            var connectionString = ConnectionStringHelper.GetEnvConfiguredConnectionStringForPersistence(tableApiType);
            var tableServiceClient = new TableServiceClient(connectionString);

            var subscriptionTableName = $"{$"{Path.GetFileNameWithoutExtension(Path.GetTempFileName())}{DateTime.UtcNow.Ticks}".ToLowerInvariant()}{AzureSubscriptionStorageDefaults.TableName}";

            var tableClient = tableServiceClient.GetTableClient(subscriptionTableName);
            await tableClient.CreateIfNotExistsAsync();

            return new Scope(new AzureSubscriptionStorage(
                new TableServiceClientForSubscriptionsFromConnectionString(connectionString),
                subscriptionTableName,
                TimeSpan.FromSeconds(10)),
                tableServiceClient,
                subscriptionTableName);
        }

        internal class Scope : IAsyncDisposable
        {
            readonly TableServiceClient tableServiceClient;
            readonly string tableName;

            public Scope(AzureSubscriptionStorage storage, TableServiceClient tableServiceClient, string tableName)
            {
                Storage = storage;
                this.tableServiceClient = tableServiceClient;
                this.tableName = tableName;
            }

            public AzureSubscriptionStorage Storage { get; }

            public async ValueTask DisposeAsync() =>
                await tableServiceClient.DeleteTableAsync(tableName);
        }
    }
}