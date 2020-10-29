using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.Sagas;
using NServiceBus.Persistence.AzureStorage.Testing;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

public class ConfigureEndpointAzureStoragePersistence : IConfigureEndpointTestExecution
{
    static string ConnectionString => Utilities.GetEnvConfiguredConnectionStringForPersistence();

    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        var persistence = configuration.UsePersistence<AzureStoragePersistence, StorageType.Sagas>();
        persistence.ConnectionString(ConnectionString);
        persistence.DefaultTable(SetupFixture.TableName);

        var recoverabilitySettings = configuration.Recoverability();

        if (endpointName != Conventions.EndpointNamingConvention(typeof(When_saga_started_concurrently.ConcurrentHandlerEndpoint)))
        {
            recoverabilitySettings.Immediate(c => c.NumberOfRetries(1));
        }

        return Task.FromResult(0);
    }

    Task IConfigureEndpointTestExecution.Cleanup()
    {
        return Task.FromResult(0);
    }
}