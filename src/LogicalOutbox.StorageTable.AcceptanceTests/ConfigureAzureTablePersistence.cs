using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.Sagas;
using NServiceBus.Persistence.AzureTable;
using NServiceBus.Pipeline;
using NServiceBus.Settings;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

public class ConfigureEndpointAzureTablePersistence : IConfigureEndpointTestExecution
{
    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        var sagaPersistence = configuration.UsePersistence<AzureTablePersistence, StorageType.Sagas>();
        sagaPersistence.UseCloudTableClient(SetupFixture.TableClient);
        sagaPersistence.Migration().DisableSecondaryKeyLookupForSagasCorrelatedByProperties();

        configuration.UsePersistence<AzureTablePersistence, StorageType.Outbox>();

        var recoverabilitySettings = configuration.Recoverability();

        if (endpointName != Conventions.EndpointNamingConvention(typeof(When_saga_started_concurrently.ConcurrentHandlerEndpoint)))
        {
            recoverabilitySettings.Immediate(c => c.NumberOfRetries(1));
        }

        configuration.Pipeline.Register(new PartitionKeyProviderBehavior.PartitionKeyProviderBehaviorRegisterStep());

        return Task.FromResult(0);
    }

    Task IConfigureEndpointTestExecution.Cleanup()
    {
        return Task.FromResult(0);
    }

    class PartitionKeyProviderBehavior : Behavior<IIncomingLogicalMessageContext>
    {
        private readonly ScenarioContext scenarioContext;
        private readonly ReadOnlySettings settings;

        public PartitionKeyProviderBehavior(ScenarioContext scenarioContext, ReadOnlySettings settings)
        {
            this.settings = settings;
            this.scenarioContext = scenarioContext;
        }

        public override Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
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

        public class PartitionKeyProviderBehaviorRegisterStep : RegisterStep
        {
            public PartitionKeyProviderBehaviorRegisterStep() : base(nameof(PartitionKeyProviderBehavior),
                typeof(PartitionKeyProviderBehavior),
                "Populates the partition key",
                builder => new PartitionKeyProviderBehavior(builder.Build<ScenarioContext>(), builder.Build<ReadOnlySettings>()))
            {
                InsertBeforeIfExists(nameof(LogicalOutboxBehavior));
            }
        }
    }
}