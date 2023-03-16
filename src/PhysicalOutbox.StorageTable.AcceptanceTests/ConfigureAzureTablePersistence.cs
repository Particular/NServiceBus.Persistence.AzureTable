using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.Sagas;
using NServiceBus.Pipeline;
using NServiceBus.Settings;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

public class ConfigureAzureTablePersistence : IConfigureEndpointTestExecution
{
    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        var sagaPersistence = configuration.UsePersistence<AzureTablePersistence, StorageType.Sagas>();
        sagaPersistence.DisableTableCreation();
        sagaPersistence.UseCloudTableClient(SetupFixture.TableClient);

        sagaPersistence.Compatibility().DisableSecondaryKeyLookupForSagasCorrelatedByProperties();

        configuration.UsePersistence<AzureTablePersistence, StorageType.Outbox>();

        var recoverabilitySettings = configuration.Recoverability();

        if (endpointName != Conventions.EndpointNamingConvention(typeof(When_saga_started_concurrently.ConcurrentHandlerEndpoint)))
        {
            recoverabilitySettings.Immediate(c => c.NumberOfRetries(1));
        }

        // This populates the partition key at the physical stage to test the conventional outbox use-case
        if (!settings.TryGet<DoNotRegisterDefaultPartitionKeyProvider>(out _))
        {
            configuration.Pipeline.Register(typeof(PartitionKeyProviderBehavior), "Populates the partition key");
        }

        if (!settings.TryGet<DoNotRegisterDefaultTableNameProvider>(out _))
        {
            configuration.Pipeline.Register(typeof(TableInformationProviderBehavior), "Populates the table information key");
        }


        return Task.CompletedTask;
    }

    Task IConfigureEndpointTestExecution.Cleanup()
    {
        return Task.CompletedTask;
    }

    class PartitionKeyProviderBehavior : Behavior<ITransportReceiveContext>
    {
        public PartitionKeyProviderBehavior(ScenarioContext scenarioContext) => this.scenarioContext = scenarioContext;

        public override Task Invoke(ITransportReceiveContext context, Func<Task> next)
        {
            if (!context.Extensions.TryGet<TableEntityPartitionKey>(out _))
            {
                context.Extensions.Set(new TableEntityPartitionKey(scenarioContext.TestRunId.ToString()));
            }

            return next();
        }

        readonly ScenarioContext scenarioContext;
    }

    class TableInformationProviderBehavior : Behavior<ITransportReceiveContext>
    {
        public TableInformationProviderBehavior(IReadOnlySettings settings) => this.settings = settings;

        public override Task Invoke(ITransportReceiveContext context, Func<Task> next)
        {
            if (!settings.TryGet<TableInformation>(out _) && !context.Extensions.TryGet<TableInformation>(out _))
            {
                context.Extensions.Set(new TableInformation(SetupFixture.TableName));
            }
            return next();
        }

        readonly IReadOnlySettings settings;
    }
}