namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Data.Tables;
    using Azure.Data.Tables.Models;
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

            for (var i = 0; i < 12; i++)
            {
                try
                {
                    TableServiceClient = new TableServiceClient(ConnectionString);
                    TableClient = TableServiceClient.GetTableClient(TableName);
                    var response = await TableClient.CreateIfNotExistsAsync();
                    Assert.That(response.Value, Is.Not.Null);
                    return;
                }
                catch (RequestFailedException e)
                {
                    var response = e.GetRawResponse();
                    if (response?.Status == 403)
                    {
                        Console.WriteLine($"Create table failed with Status 403 ({response.ReasonPhrase}), will wait 15s up to 3m");
                        await Task.Delay(TimeSpan.FromSeconds(15));
                    }
                    else
                    {
                        throw;
                    }
                }
            }
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

        public static string TableName { get; private set; }
        public static TableServiceClient TableServiceClient { get; private set; }
        public static TableClient TableClient { get; private set; }
    }
}