using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.AcceptanceTests.Routing.MessageDrivenSubscriptions;
using NServiceBus.AcceptanceTests.Sagas;
using NServiceBus.AcceptanceTests.ScenarioDescriptors;
using NServiceBus.Persistence;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

public class ConfigureEndpointAzureStoragePersistence : IConfigureEndpointTestExecution
{
    static string ConnectionString => EnvironmentHelper.GetEnvironmentVariable($"{nameof(AzureStoragePersistence)}.ConnectionString") ?? "UseDevelopmentStorage=true";

    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        configuration.UsePersistence<AzureStoragePersistence, StorageType.Subscriptions>().ConnectionString(ConnectionString);
        configuration.UsePersistence<AzureStoragePersistence, StorageType.Timeouts>().ConnectionString(ConnectionString);
        var sagaPersistence = configuration.UsePersistence<AzureStoragePersistence, StorageType.Sagas>().ConnectionString(ConnectionString);
        sagaPersistence.AssumeSecondaryIndicesExist(true);

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