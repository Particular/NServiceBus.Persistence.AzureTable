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
            ConnectionString = this.GetEnvConfiguredConnectionStringByCallerConvention();

            TableName = $"{Path.GetFileNameWithoutExtension(Path.GetTempFileName())}{DateTime.UtcNow.Ticks}".ToLowerInvariant();

            TableServiceClient = new TableServiceClient(ConnectionString);
            TableClient = TableServiceClient.GetTableClient(TableName);
            var response = await TableClient.CreateIfNotExistsAsync();
            Assert.That(response.Value, Is.Not.Null);
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            ConnectionString = null;

            try
            {
                var response = await TableServiceClient.DeleteTableAsync(TableName);
                Assert.That(response.IsError, Is.False);
            }
            catch (RequestFailedException e) when (e.Status == (int)HttpStatusCode.NotFound)
            {
            }
        }

        public static string ConnectionString { get; private set; }

        public static string TableName { get; private set; }
        public static TableServiceClient TableServiceClient { get; private set; }
        public static TableClient TableClient { get; private set; }
    }
}