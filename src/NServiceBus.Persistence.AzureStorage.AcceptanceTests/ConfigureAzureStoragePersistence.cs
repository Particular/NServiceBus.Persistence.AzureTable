using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
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
        configuration.UsePersistence<AzureStoragePersistence, StorageType.Sagas>().ConnectionString(ConnectionString);
        configuration.UsePersistence<AzureStoragePersistence, StorageType.Timeouts>().ConnectionString(ConnectionString);

        if (endpointName == Conventions.EndpointNamingConvention(typeof(When_a_base_class_message_starts_a_saga.SagaEndpoint))
            || endpointName == Conventions.EndpointNamingConvention(typeof(When_finder_returns_existing_saga.SagaEndpoint)))
        {
            configuration.Recoverability().Immediate(c => c.NumberOfRetries(1));
        }

        if (endpointName == Conventions.EndpointNamingConvention(typeof(When_saga_started_concurrently.ConcurrentHandlerEndpoint)))
        {
            configuration.Recoverability().Immediate(c => c.NumberOfRetries(4));
        }

        return Task.FromResult(0);
    }

    Task IConfigureEndpointTestExecution.Cleanup()
    {
        return Task.FromResult(0);
    }
}