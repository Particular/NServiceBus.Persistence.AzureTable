﻿namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.AcceptanceTests.Sagas;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using Conventions = AcceptanceTesting.Customization.Conventions;

    public class ConfigureEndpointAzureTablePersistence : IConfigureEndpointTestExecution
    {
        Task IConfigureEndpointTestExecution.Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
        {
            var sagaPersistence = configuration.UsePersistence<AzureTablePersistence, StorageType.Sagas>();
            // backdoor for testing
            sagaPersistence.GetSettings().Set(Persistence.AzureTable.WellKnownConfigurationKeys.SagaStorageConventionalTablePrefix, SetupFixture.TablePrefix);

            sagaPersistence.UseTableServiceClient(SetupFixture.TableServiceClient);

            var recoverabilitySettings = configuration.Recoverability();

            if (endpointName != Conventions.EndpointNamingConvention(typeof(When_saga_started_concurrently.ConcurrentHandlerEndpoint)))
            {
                recoverabilitySettings.Immediate(c => c.NumberOfRetries(1));
            }
            else
            {
                // due to races on the table creation with cosmos table API we need go through some delayed retries in addition
                // to the already configured immediate retries
                recoverabilitySettings.Delayed(c => c.NumberOfRetries(3).TimeIncrease(TimeSpan.FromSeconds(5)));
            }

            return Task.FromResult(0);
        }

        Task IConfigureEndpointTestExecution.Cleanup() => Task.FromResult(0);
    }
}