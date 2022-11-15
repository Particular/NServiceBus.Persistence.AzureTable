namespace NServiceBus.PersistenceTesting
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
            ConnectionString = this.GetEnvConfiguredConnectionStringByCallerConvention();

            TableName = $"{Path.GetFileNameWithoutExtension(Path.GetTempFileName())}{DateTime.UtcNow.Ticks}".ToLowerInvariant();

            TableServiceClient = new TableServiceClient(ConnectionString);
            TableClient = TableServiceClient.GetTableClient(TableName);
            await TableClient.CreateIfNotExistsAsync();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            ConnectionString = null;
            try
            {
                await TableServiceClient.DeleteTableAsync(TableName);
            }
            catch (RequestFailedException e) when (e.Status == (int)HttpStatusCode.NotFound)
            {
            }
        }

        public static string ConnectionString { get; private set; }

        public static string TableName;
        public static TableServiceClient TableServiceClient;
        public static TableClient TableClient;
    }
}