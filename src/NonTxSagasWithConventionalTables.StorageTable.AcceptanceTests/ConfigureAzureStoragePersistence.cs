using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.Sagas;
using NServiceBus.Configuration.AdvancedExtensibility;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

public class ConfigureEndpointAzureTablePersistence : IConfigureEndpointTestExecution
{
    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        var persistence = configuration.UsePersistence<AzureTablePersistence, StorageType.Sagas>();
        // backdoor for testing
        persistence.GetSettings().Set("AzureSagaStorage.ConventionalTablePrefix", SetupFixture.TablePrefix);

        persistence.UseTableServiceClient(SetupFixture.TableClient);

        persistence.Compatibility().DisableSecondaryKeyLookupForSagasCorrelatedByProperties();

        var recoverabilitySettings = configuration.Recoverability();

        if (endpointName != Conventions.EndpointNamingConvention(typeof(When_saga_started_concurrently.ConcurrentHandlerEndpoint)))
        {
            recoverabilitySettings.Immediate(c => c.NumberOfRetries(1));
        }
        else
        {
            // due to races on the table creation with cosmos table API we need go through some delayed retries in addition
            // to the already configured immediate retries
            recoverabilitySettings.Delayed(c =>
            {
                c.NumberOfRetries(3);
                c.TimeIncrease(TimeSpan.FromSeconds(5));
            });
        }

        return Task.FromResult(0);
    }

    Task IConfigureEndpointTestExecution.Cleanup()
    {
        return Task.FromResult(0);
    }
}