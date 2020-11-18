namespace NServiceBus.AcceptanceTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Microsoft.Azure.Cosmos.Table;
    using Testing;

    [SetUpFixture]
    public class SetupFixture
    {
        [OneTimeSetUp]
        public Task OneTimeSetUp()
        {
            var connectionString = this.GetEnvConfiguredConnectionStringByCallerConvention();

            var account = CloudStorageAccount.Parse(connectionString);
            TableClient = account.CreateCloudTableClient();

            return Task.CompletedTask;
        }

        [OneTimeTearDown]
        public Task OneTimeTearDown()
        {
            // we can't delete the tables due to conflicts when recreating
            return Task.CompletedTask;
        }

        public static CloudTableClient TableClient;
    }
}