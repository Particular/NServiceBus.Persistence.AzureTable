namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Microsoft.Azure.Cosmos.Table;
    using Testing;

    [SetUpFixture]
    public class SetupFixture
    {
        [OneTimeSetUp]
        public Task OneTimeSetUp()
        {
            var connectionString = this.GetEnvConfiguredConnectionStringByCallerConvention();

            var account = CloudStorageAccount.Parse(connectionString);
            TableClient = account.CreateCloudTableClient();

            TablePrefix = $"{Path.GetFileNameWithoutExtension(Path.GetTempFileName())}{DateTime.UtcNow.Ticks}".ToLowerInvariant();

            allConventionalSagaTableNamesWithPrefix = GetType().Assembly.GetTypes().Where(x => x.GetInterfaces().Contains(typeof(IContainSagaData)))
                .Select(x => $"{TablePrefix}{x.Name}").ToArray();

            return Task.CompletedTask;
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
        string[] allConventionalSagaTableNamesWithPrefix;
    }
}