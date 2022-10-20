namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Data.Tables;
    using NUnit.Framework;
    using Testing;

    [SetUpFixture]
    public class SetupFixture
    {
        [OneTimeSetUp]
        public Task OneTimeSetUp()
        {
            var connectionString = this.GetEnvConfiguredConnectionStringByCallerConvention();

            TableClient = new TableServiceClient(connectionString);

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
                var table = TableClient.GetTableClient(tableName);
                try
                {
                    return TableClient.DeleteTableAsync(tableName);
                }
                catch (RequestFailedException e) when (e.Status == (int)HttpStatusCode.NotFound)
                {
                    return Task.CompletedTask;
                }
            }).ToArray());
        }

        public static TableServiceClient TableClient;
        public static string TablePrefix;
        string[] allConventionalSagaTableNamesWithPrefix;
    }
}