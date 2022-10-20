namespace NServiceBus.TransactionalSession.AcceptanceTests
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
            var connectionString = this.GetEnvConfiguredConnectionStringByCallerConvention();

            TableName = $"{Path.GetFileNameWithoutExtension(Path.GetTempFileName())}{DateTime.UtcNow.Ticks}".ToLowerInvariant();

            TableClient = new TableServiceClient(connectionString);
            Table = TableClient.GetTableClient(TableName);
            await Table.CreateIfNotExistsAsync();
        }

        [OneTimeTearDown]
        public Task OneTimeTearDown()
        {
            try
            {
                return TableClient.DeleteTableAsync(TableName);
            }
            catch (RequestFailedException e) when (e.Status == (int)HttpStatusCode.NotFound)
            {
                return Task.CompletedTask;
            }
        }

        public static string TableName;
        public static TableServiceClient TableClient;
        public static TableClient Table;
    }
}