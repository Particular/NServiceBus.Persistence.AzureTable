namespace NServiceBus.AzureStoragePersistence.ComponentTests.Persisters
{
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NUnit.Framework;
    using NServiceBus;

    [TestFixture]
    [Category("Azure")]
    public class When_using_GlobalConnectionStringConfiguration
    {
        [Test]
        public void Specifying_a_single_connection_string_should_populate_all_connection_string_settings()
        {
            var connectionString = "UseDevelopmentStorage=true";

            var endpointConfig = new EndpointConfiguration();
            var persistence = endpointConfig.UsePersistence<AzureStoragePersistence>().ConnectionString(connectionString);

            var settings = persistence.GetSettings();

            Assert.AreEqual(connectionString, settings.Get<string>("AzureSagaStorage.ConnectionString"));
            Assert.AreEqual(connectionString, settings.Get<string>("AzureSubscriptionStorage.ConnectionString"));
            Assert.AreEqual(connectionString, settings.Get<string>("AzureTimeoutStorage.ConnectionString"));
        }
    }
}