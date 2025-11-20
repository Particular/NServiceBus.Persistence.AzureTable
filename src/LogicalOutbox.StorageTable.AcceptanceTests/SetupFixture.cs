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

            const int minutes = 5;

            for (var i = 0; i < minutes * 4; i++)
            {
                try
                {
                    TableServiceClient = new TableServiceClient(ConnectionString);
                    TableClient = TableServiceClient.GetTableClient(TableName);
                    var response = await TableClient.CreateIfNotExistsAsync();
                    await TestContext.Out.WriteLineAsync($"OneTimeSetUp created table {TableName} to test Cosmos DB readiness");
                    Assert.That(response.Value, Is.Not.Null);

                    await TestContext.Out.WriteLineAsync("Waiting an additional 15s for Comsos DB to be available anyway");
                    await Task.Delay(TimeSpan.FromSeconds(15));
                    return;
                }
                catch (RequestFailedException e)
                {
                    var response = e.GetRawResponse();
                    if (response?.Status == 403)
                    {
                        await TestContext.Out.WriteLineAsync($"Create table failed with Status 403 ({response.ReasonPhrase}), will wait 15s up to {minutes}m");
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