namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Microsoft.Azure.Cosmos.Table;

    [SetUpFixture]
    public class SetupFixture
    {
        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            var connectionString = ConnectionStringHelper.GetEnvConfiguredConnectionStringForPersistence("CosmosDB");

            TableName = $"{Path.GetFileNameWithoutExtension(Path.GetTempFileName())}{DateTime.UtcNow.Ticks}".ToLowerInvariant();

            var account = CloudStorageAccount.Parse(connectionString);
            TableClient = account.CreateCloudTableClient();
            Table = TableClient.GetTableReference(TableName);
            try
            {
                await Table.CreateIfNotExistsAsync();
            }
            catch (StorageException e)
            {
                Console.WriteLine(e);
                throw;
            }

            handler = new TransactionalBatchCounterHandler();
        }

        [OneTimeTearDown]
        public Task OneTimeTearDown()
        {
            handler.Dispose();
            return Table.DeleteIfExistsAsync();
        }

        public static string TableName;
        public static CloudTableClient TableClient;
        public static CloudTable Table;
        private TransactionalBatchCounterHandler handler;
    }
}