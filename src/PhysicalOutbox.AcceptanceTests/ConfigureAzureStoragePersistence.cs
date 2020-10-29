using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.Sagas;
using NServiceBus.Persistence.AzureStorage.Testing;
using NServiceBus.Pipeline;
using NServiceBus.Settings;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

public class ConfigureEndpointAzureStoragePersistence : IConfigureEndpointTestExecution
{
    static string ConnectionString => Utilities.GetEnvConfiguredConnectionStringForPersistence();

    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        var sagaPersistence = configuration.UsePersistence<AzureStoragePersistence, StorageType.Sagas>();
        sagaPersistence.ConnectionString(ConnectionString);

        sagaPersistence.Migration().DisableSecondaryKeyLookupForSagasCorrelatedByProperties();

        var outboxPersistence = configuration.UsePersistence<AzureStoragePersistence, StorageType.Outbox>();
        outboxPersistence.ConnectionString(ConnectionString);

        var recoverabilitySettings = configuration.Recoverability();

        if (endpointName != Conventions.EndpointNamingConvention(typeof(When_saga_started_concurrently.ConcurrentHandlerEndpoint)))
        {
            recoverabilitySettings.Immediate(c => c.NumberOfRetries(1));
        }

        // This populates the partition key at the physical stage to test the conventional outbox use-case
        configuration.Pipeline.Register(typeof(PartitionKeyProviderBehavior), "Populates the partition key");

        return Task.FromResult(0);
    }

    Task IConfigureEndpointTestExecution.Cleanup()
    {
        return Task.FromResult(0);
    }

    class PartitionKeyProviderBehavior : Behavior<ITransportReceiveContext>
    {
        private readonly ScenarioContext scenarioContext;
        private readonly ReadOnlySettings settings;

        public PartitionKeyProviderBehavior(ScenarioContext scenarioContext, ReadOnlySettings settings)
        {
            this.settings = settings;
            this.scenarioContext = scenarioContext;
        }

        public override Task Invoke(ITransportReceiveContext context, Func<Task> next)
        {
            if (!context.Extensions.TryGet<TableEntityPartitionKey>(out _))
            {
                context.Extensions.Set(new TableEntityPartitionKey(scenarioContext.TestRunId.ToString()));
            }

            if (!settings.TryGet<TableInformation>(out _) && !context.Extensions.TryGet<TableInformation>(out _))
            {
                context.Extensions.Set(new TableInformation(SetupFixture.TableName));
            }
            return next();
        }
    }
}