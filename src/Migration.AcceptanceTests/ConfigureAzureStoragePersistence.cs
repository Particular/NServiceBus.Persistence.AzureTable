using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.AcceptanceTests;
using NServiceBus.Persistence.AzureStorage.Testing;

public class ConfigureEndpointAzureStoragePersistence : IConfigureEndpointTestExecution
{
    static string ConnectionString => Utilities.GetEnvConfiguredConnectionStringForPersistence();

    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        var persistence = configuration.UsePersistence<AzureStoragePersistence, StorageType.Sagas>();
        persistence.UseCloudTableClient(SetupFixture.TableClient);

        return Task.FromResult(0);
    }

    Task IConfigureEndpointTestExecution.Cleanup()
    {
        return Task.FromResult(0);
    }
}