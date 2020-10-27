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
            var connectionString = GetEnvConfiguredConnectionStringForPersistence();

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

        public static string GetEnvConfiguredConnectionStringForPersistence()
        {
            var environmentVartiableName = "AzureStoragePersistence_ConnectionString";
            var connectionString = GetEnvironmentVariable(environmentVartiableName);
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception($"Oh no! We couldn't find an environment variable '{environmentVartiableName}' with Azure Storage connection string.");
            }

            return connectionString;
        }

        static string GetEnvironmentVariable(string variable)
        {
            var candidate = Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.User);
            return string.IsNullOrWhiteSpace(candidate) ? Environment.GetEnvironmentVariable(variable) : candidate;
        }

        public static string TableName;
        public static CloudTableClient TableClient;
        public static CloudTable Table;
        private TransactionalBatchCounterHandler handler;
    }
}