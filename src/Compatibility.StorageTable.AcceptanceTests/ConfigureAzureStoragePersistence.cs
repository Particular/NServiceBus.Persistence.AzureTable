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

        persistence.DisableTableCreation();
        persistence.UseTableServiceClient(SetupFixture.TableServiceClient);

        return Task.CompletedTask;
    }

    Task IConfigureEndpointTestExecution.Cleanup() => Task.CompletedTask;
}