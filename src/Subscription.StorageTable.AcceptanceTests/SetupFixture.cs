namespace NServiceBus.AcceptanceTests
{
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

            TableName = $"{Path.GetFileNameWithoutExtension(Path.GetTempFileName())}{DateTime.UtcNow.Ticks}".ToLowerInvariant();

            var account = CloudStorageAccount.Parse(connectionString);
            TableClient = account.CreateCloudTableClient();
            Table = TableClient.GetTableReference(TableName);
            await Table.CreateIfNotExistsAsync();
        }

        [OneTimeTearDown]
        public Task OneTimeTearDown()
        {
            return Table.DeleteIfExistsAsync();
        }

        public static string TableName;
        public static CloudTableClient TableClient;
        public static CloudTable Table;
    }
}