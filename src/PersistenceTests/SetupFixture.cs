namespace NServiceBus.PersistenceTesting
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Microsoft.Azure.Cosmos.Table;
    using AcceptanceTests;

    [SetUpFixture]
    public class SetupFixture
    {
        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            var connectionString = ConnectionStringHelper.GetEnvConfiguredConnectionStringForPersistence();

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