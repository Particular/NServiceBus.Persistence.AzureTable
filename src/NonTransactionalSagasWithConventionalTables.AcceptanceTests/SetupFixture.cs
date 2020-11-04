namespace NServiceBus.AcceptanceTests
{
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Microsoft.Azure.Cosmos.Table;
    using Testing;

    [SetUpFixture]
    public class SetupFixture
    {
        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            var connectionString = ConnectionStringHelper.GetEnvConfiguredConnectionStringForPersistence();

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

        public static CloudTableClient TableClient;
        private string[] allSagaDataTypeNames;
    }
}