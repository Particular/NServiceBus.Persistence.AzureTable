namespace NServiceBus.AcceptanceTests
{
    using System.Linq;
    using System;
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

            var account = CloudStorageAccount.Parse(connectionString);
            TableClient = account.CreateCloudTableClient();

            allSagaDataTypeNames = GetType().Assembly.GetTypes().Where(x => x.GetInterfaces().Contains(typeof(IContainSagaData)))
                .Select(x => x.Name).ToArray();

            foreach (var dataTypeName in allSagaDataTypeNames)
            {
                var table = TableClient.GetTableReference(dataTypeName);
                await table.CreateIfNotExistsAsync();
            }
        }

        [OneTimeTearDown]
        public Task OneTimeTearDown()
        {
            // we can't delete the tables due to conflicts when recreating
            return Task.CompletedTask;
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

        public static CloudTableClient TableClient;
        private string[] allSagaDataTypeNames;
    }
}