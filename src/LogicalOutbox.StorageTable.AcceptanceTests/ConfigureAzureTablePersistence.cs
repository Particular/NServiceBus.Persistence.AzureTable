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
        sagaPersistence.DisableTableCreation();
        sagaPersistence.UseCloudTableClient(SetupFixture.TableClient);
        sagaPersistence.Compatibility().DisableSecondaryKeyLookupForSagasCorrelatedByProperties();

        configuration.UsePersistence<AzureTablePersistence, StorageType.Outbox>();

        var recoverabilitySettings = configuration.Recoverability();

        if (endpointName != Conventions.EndpointNamingConvention(typeof(When_saga_started_concurrently.ConcurrentHandlerEndpoint)))
        {
            recoverabilitySettings.Immediate(c => c.NumberOfRetries(1));
        }

        if (!settings.TryGet<DoNotRegisterDefaultPartitionKeyProvider>(out _))
        {
            configuration.Pipeline.Register(new PartitionKeyProviderBehavior.Registration());
        }
        if (!settings.TryGet<DoNotRegisterDefaultTableNameProvider>(out _))
        {
            configuration.Pipeline.Register(new TableInformationProviderBehavior.Registration());
        }

        return Task.CompletedTask;
    }

    Task IConfigureEndpointTestExecution.Cleanup()
    {
        return Task.CompletedTask;
    }

   class PartitionKeyProviderBehavior : Behavior<IIncomingLogicalMessageContext>
    {
        public PartitionKeyProviderBehavior(ScenarioContext scenarioContext) => this.scenarioContext = scenarioContext;

        public override Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
        {
            if (!context.Extensions.TryGet<TableEntityPartitionKey>(out _))
            {
                context.Extensions.Set(new TableEntityPartitionKey(scenarioContext.TestRunId.ToString()));
            }

            return next();
        }

        readonly ScenarioContext scenarioContext;

        public class Registration : RegisterStep
        {
            public Registration() : base(nameof(PartitionKeyProviderBehavior),
                typeof(PartitionKeyProviderBehavior),
                "Populates the partition key",
                provider => new PartitionKeyProviderBehavior(provider.Build<ScenarioContext>())) =>
                InsertBeforeIfExists(nameof(LogicalOutboxBehavior));
        }
    }

    class TableInformationProviderBehavior : Behavior<IIncomingLogicalMessageContext>
    {
        public TableInformationProviderBehavior(ReadOnlySettings settings) => this.settings = settings;

        public override Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
        {
            if (!settings.TryGet<TableInformation>(out _) && !context.Extensions.TryGet<TableInformation>(out _))
            {
                context.Extensions.Set(new TableInformation(SetupFixture.TableName));
            }
            return next();
        }

        readonly ReadOnlySettings settings;

        public class Registration : RegisterStep
        {
            public Registration() : base(nameof(TableInformationProviderBehavior),
                typeof(TableInformationProviderBehavior),
                "Populates the table information",
                provider => new TableInformationProviderBehavior(provider.Build<ReadOnlySettings>())) =>
                InsertBeforeIfExists(nameof(LogicalOutboxBehavior));
        }
    }
}