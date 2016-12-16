namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Persisters
{
    using Config;
    using Configuration.AdvanceExtensibility;
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

            var endpointConfig = new EndpointConfiguration("Testendpoint");
            var persistence = endpointConfig.UsePersistence<AzureStoragePersistence>().ConnectionString(connectionString);

            var settings = persistence.GetSettings();

            Assert.AreEqual(connectionString, settings.Get<string>(WellKnownConfigurationKeys.SagaStorageConnectionString));
            Assert.AreEqual(connectionString, settings.Get<string>(WellKnownConfigurationKeys.SubscriptionStorageConnectionString));
            Assert.AreEqual(connectionString, settings.Get<string>(WellKnownConfigurationKeys.TimeoutStorageConnectionString));
        }
    }
}