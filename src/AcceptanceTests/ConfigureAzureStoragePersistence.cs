using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.AcceptanceTests.Routing.MessageDrivenSubscriptions;
using NServiceBus.AcceptanceTests.Sagas;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

public class ConfigureEndpointAzureStoragePersistence : IConfigureEndpointTestExecution
{
    static string ConnectionString => Testing.Utillities.GetEnvConfiguredConnectionStringForPersistence();

    // TODO: needs to be conditional to accomodate testing of persistence with CosmosDB and Azure Storage only
    static string TimeoutsStateConnectionString => Testing.Utillities.GetEnvConfiguredConnectionStringForBlobStorage();

    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        configuration.UsePersistence<AzureStoragePersistence, StorageType.Subscriptions>().ConnectionString(ConnectionString);
        configuration.UsePersistence<AzureStoragePersistence, StorageType.Sagas>().ConnectionString(ConnectionString);
        var timeoutPersistence = configuration.UsePersistence<AzureStoragePersistence, StorageType.Timeouts>();
        timeoutPersistence.ConnectionString(ConnectionString);
        timeoutPersistence.TimeoutStageStorageConnectionString(TimeoutsStateConnectionString);

        var recoverabilitySettings = configuration.Recoverability();

        if (endpointName != Conventions.EndpointNamingConvention(typeof(When_multi_subscribing_to_a_polymorphic_event.Publisher1))
            || endpointName != Conventions.EndpointNamingConvention(typeof(When_multi_subscribing_to_a_polymorphic_event.Publisher2))
            || endpointName != Conventions.EndpointNamingConvention(typeof(When_multi_subscribing_to_a_polymorphic_event.Publisher2))
            || endpointName != Conventions.EndpointNamingConvention(typeof(When_saga_started_concurrently.ConcurrentHandlerEndpoint)))
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