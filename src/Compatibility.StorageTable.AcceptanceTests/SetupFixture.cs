namespace NServiceBus.AcceptanceTests
{
    using System.Linq;
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Azure.Data.Tables;
    using NUnit.Framework;
    using Testing;

    [SetUpFixture]
    public class SetupFixture
    {
        [OneTimeSetUp]
        public Task OneTimeSetUp()
        {
            ConnectionString = this.GetEnvConfiguredConnectionStringByCallerConvention();

            TableServiceClient = new TableServiceClient(ConnectionString);

            TablePrefix = $"{Path.GetFileNameWithoutExtension(Path.GetTempFileName())}{DateTime.UtcNow.Ticks}".ToLowerInvariant();

            allConventionalSagaTableNamesWithPrefix = GetType().Assembly.GetTypes().Where(x => x.GetInterfaces().Contains(typeof(IContainSagaData)))
                .Select(x => $"{TablePrefix}{x.Name}").ToArray();

            return Task.WhenAll(allConventionalSagaTableNamesWithPrefix
                .Select(tableName => TableServiceClient.CreateTableIfNotExistsAsync(tableName))
                .ToArray());
        }

        [OneTimeTearDown]
        public Task OneTimeTearDown() => Task.WhenAll(allConventionalSagaTableNamesWithPrefix
            .Select(tableName => TableServiceClient.DeleteTableAsync(tableName))
            .ToArray());

        public static string ConnectionString { get; private set; }
        public static TableServiceClient TableServiceClient { get; private set; }
        public static string TablePrefix;
        string[] allConventionalSagaTableNamesWithPrefix;
    }
}