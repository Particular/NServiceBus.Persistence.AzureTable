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
            ConnectionString = this.GetEnvConfiguredConnectionStringByCallerConvention();

            TableServiceClient = new TableServiceClient(ConnectionString);

            TablePrefix = $"{Path.GetFileNameWithoutExtension(Path.GetTempFileName())}{DateTime.UtcNow.Ticks}".ToLowerInvariant();

            allConventionalSagaTableNamesWithPrefix = GetType().Assembly.GetTypes().Where(x => x.GetInterfaces().Contains(typeof(IContainSagaData)))
                .Select(x => $"{TablePrefix}{x.Name}").ToArray();

            return Task.CompletedTask;
        }

        [OneTimeTearDown]
        public Task OneTimeTearDown()
        {
            ConnectionString = null;
            return Task.WhenAll(allConventionalSagaTableNamesWithPrefix.Select(async tableName =>
            {
                try
                {
                    await TableServiceClient.DeleteTableAsync(tableName);
                }
                catch (RequestFailedException e) when (e.Status == (int)HttpStatusCode.NotFound)
                {
                }
            }).ToArray());
        }

        public static string ConnectionString { get; private set; }

        public static TableServiceClient TableServiceClient { get; private set; }
        public static string TablePrefix { get; private set; }
        string[] allConventionalSagaTableNamesWithPrefix;
    }
}