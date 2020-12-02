using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.AcceptanceTests;
using NServiceBus.Configuration.AdvancedExtensibility;

public class ConfigureEndpointAzureTablePersistence : IConfigureEndpointTestExecution
{
    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        var persistence = configuration.UsePersistence<AzureTablePersistence, StorageType.Sagas>();
        // backdoor for testing
        persistence.GetSettings().Set("AzureSagaStorage.ConventionalTablePrefix", SetupFixture.TablePrefix);

        persistence.UseCloudTableClient(SetupFixture.TableClient);

        persistence.Compatibility().DisableSecondaryKeyLookupForSagasCorrelatedByProperties();

        return Task.FromResult(0);
    }

    Task IConfigureEndpointTestExecution.Cleanup()
    {
        return Task.FromResult(0);
    }
}