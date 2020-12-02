namespace NServiceBus.Persistence.AzureTable.Tests
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Testing;

    public static class SubscriptionTestHelper
    {
        internal static async Task<Scope> CreateAzureSubscriptionStorage(string tableApiType)
        {
            var connectionString = ConnectionStringHelper.GetEnvConfiguredConnectionStringForPersistence(tableApiType);
            var account = CloudStorageAccount.Parse(connectionString);

            var subscriptionTableName = $"{$"{Path.GetFileNameWithoutExtension(Path.GetTempFileName())}{DateTime.UtcNow.Ticks}".ToLowerInvariant()}{AzureSubscriptionStorageDefaults.TableName}";

            var table = account.CreateCloudTableClient().GetTableReference(subscriptionTableName);
            await table.CreateIfNotExistsAsync();

            return new Scope(new AzureSubscriptionStorage(
                new CloudTableClientForSubscriptionsFromConnectionString(connectionString),
                subscriptionTableName,
                TimeSpan.FromSeconds(10)),
                table);
        }

        internal class Scope : IDisposable
        {
            private readonly CloudTable cloudTable;

            public Scope(AzureSubscriptionStorage storage, CloudTable cloudTable)
            {
                Storage = storage;
                this.cloudTable = cloudTable;
            }

            public AzureSubscriptionStorage Storage { get; }

            public void Dispose()
            {
                // unfortunately we don't have IAsyncDisposable yet.
                cloudTable.DeleteIfExists();
            }
        }
    }
}