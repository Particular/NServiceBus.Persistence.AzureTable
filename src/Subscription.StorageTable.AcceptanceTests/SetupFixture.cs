namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.IO;
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
        public async Task OneTimeSetUp()
        {
            // ensure the persistence assembly is loaded into the AppDomain because it needs its features to be scanned to work properly.
            typeof(AzureTablePersistence).ToString();

            ConnectionString = this.GetEnvConfiguredConnectionStringByCallerConvention();

            TableName = $"{Path.GetFileNameWithoutExtension(Path.GetTempFileName())}{DateTime.UtcNow.Ticks}".ToLowerInvariant();

            TableClient = new TableServiceClient(ConnectionString);
            Table = TableClient.GetTableClient(TableName);
            await Table.CreateIfNotExistsAsync();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            ConnectionString = null;
            try
            {
                await TableClient.DeleteTableAsync(TableName);
            }
            catch (RequestFailedException e) when (e.Status == (int)HttpStatusCode.NotFound)
            {
            }
        }

        public static string ConnectionString { get; private set; }

        public static string TableName;
        public static TableServiceClient TableClient;
        public static TableClient Table;
    }
}