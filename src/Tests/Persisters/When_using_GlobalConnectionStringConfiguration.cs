namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Persisters
{
    using Config;
    using Configuration.AdvancedExtensibility;
    using NUnit.Framework;
    using NServiceBus;

    [TestFixture]
    [Category("Azure")]
    public class When_using_GlobalConnectionStringConfiguration
    {
        [Test]
        public void Specifying_a_single_connection_string_should_populate_all_connection_string_settings_but_timeouts_state_connection_string()
        {
            var connectionString = "UseDevelopmentStorage=true";

            var endpointConfig = new EndpointConfiguration("Testendpoint");
            var persistence = endpointConfig.UsePersistence<AzureStoragePersistence>().ConnectionString(connectionString);

            var settings = persistence.GetSettings();

            Assert.AreEqual(connectionString, settings.Get<string>(WellKnownConfigurationKeys.SagaStorageConnectionString));
            Assert.AreEqual(connectionString, settings.Get<string>(WellKnownConfigurationKeys.SubscriptionStorageConnectionString));
            Assert.AreEqual(connectionString, settings.Get<string>(WellKnownConfigurationKeys.TimeoutStorageConnectionString));
            Assert.AreEqual(false, settings.HasSetting(WellKnownConfigurationKeys.TimeoutStateStorageConnectionString));
        }

        [Test]
        public void Specifying_a_separate_connection_string_for_timeouts_state_should_work()
        {
            var cosmosDbConnectionString = "cosmosDB";
            var blobConnectionString = "storage";

            var endpointConfig = new EndpointConfiguration("Testendpoint");
            var persistence = endpointConfig.UsePersistence<AzureStoragePersistence>();
            persistence.ConnectionString(cosmosDbConnectionString);
            persistence.TimeoutStageStorageConnectionString(blobConnectionString);

            var settings = persistence.GetSettings();

            Assert.AreEqual(cosmosDbConnectionString, settings.Get<string>(WellKnownConfigurationKeys.SagaStorageConnectionString));
            Assert.AreEqual(cosmosDbConnectionString, settings.Get<string>(WellKnownConfigurationKeys.SubscriptionStorageConnectionString));
            Assert.AreEqual(cosmosDbConnectionString, settings.Get<string>(WellKnownConfigurationKeys.TimeoutStorageConnectionString));
            Assert.AreEqual(blobConnectionString, settings.Get<string>(WellKnownConfigurationKeys.TimeoutStateStorageConnectionString));
        }
    }


}