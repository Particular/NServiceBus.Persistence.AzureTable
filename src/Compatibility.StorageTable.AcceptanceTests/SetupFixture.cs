namespace NServiceBus.AcceptanceTests
{
    using System.Linq;
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Microsoft.Azure.Cosmos.Table;
    using Testing;

    [SetUpFixture]
    public class SetupFixture
    {
        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            var connectionString = this.GetEnvConfiguredConnectionStringByCallerConvention();

            var account = CloudStorageAccount.Parse(connectionString);
            TableClient = account.CreateCloudTableClient();

            TablePrefix = $"{Path.GetFileNameWithoutExtension(Path.GetTempFileName())}{DateTime.UtcNow.Ticks}".ToLowerInvariant();

            allConventionalSagaTableNamesWithPrefix = GetType().Assembly.GetTypes().Where(x => x.GetInterfaces().Contains(typeof(IContainSagaData)))
                .Select(x => $"{TablePrefix}{x.Name}").ToArray();

            await Task.WhenAll(allConventionalSagaTableNamesWithPrefix.Select(tableName =>
            {
                var table = TableClient.GetTableReference(tableName);
                return table.CreateIfNotExistsAsync();
            }).ToArray());
            
            // ensure the persistence assembly is loaded into the AppDomain because it needs its features to be scanned to work properly.
            typeof(AzureTablePersistence).ToString();
        }

        [OneTimeTearDown]
        public Task OneTimeTearDown()
        {
            return Task.WhenAll(allConventionalSagaTableNamesWithPrefix.Select(tableName =>
            {
                var table = TableClient.GetTableReference(tableName);
                return table.DeleteIfExistsAsync();
            }).ToArray());
        }

        public static CloudTableClient TableClient;
        public static string TablePrefix;
        private string[] allConventionalSagaTableNamesWithPrefix;
    }
}