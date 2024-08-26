namespace NServiceBus.Persistence.AzureTable.Tests
{
    using Configuration.AdvancedExtensibility;
    using NUnit.Framework;
    using NServiceBus;

    [TestFixture]
    public class When_using_GlobalConnectionStringConfiguration
    {
        [Test]
        public void Specifying_a_single_connection_string_should_populate_all_connection_string_settings()
        {
            var connectionString = "UseDevelopmentStorage=true";

            var endpointConfig = new EndpointConfiguration("Testendpoint");
            var persistence = endpointConfig.UsePersistence<AzureTablePersistence>().ConnectionString(connectionString);

            var settings = persistence.GetSettings();
            var provideTableServiceClient = settings.Get<IProvideTableServiceClient>();

            Assert.That(provideTableServiceClient, Is.AssignableFrom<TableServiceClientFromConnectionString>().And
                .Not.AssignableFrom<TableServiceClientFromConfiguration>().And.Not
                .AssignableFrom<TableServiceServiceClientForSubscriptionsFromConfiguration>());
        }
    }
}