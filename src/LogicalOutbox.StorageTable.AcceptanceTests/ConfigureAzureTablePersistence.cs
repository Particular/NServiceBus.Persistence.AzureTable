namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using Azure.Data.Tables;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.AcceptanceTests.Sagas;
    using NServiceBus.Persistence.AzureTable;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;
    using Conventions = AcceptanceTesting.Customization.Conventions;

    public class ConfigureAzureTablePersistence : IConfigureEndpointTestExecution
    {
        readonly TableServiceClient tableServiceClient;

        public ConfigureAzureTablePersistence(TableServiceClient tableServiceClient = null) =>
            this.tableServiceClient = tableServiceClient ?? SetupFixture.TableServiceClient;

        Task IConfigureEndpointTestExecution.Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
        {
            configuration.UsePersistence<AzureTablePersistence, StorageType.Sagas>()
                         .DisableTableCreation()
                         .UseTableServiceClient(tableServiceClient)
                         .Compatibility().DisableSecondaryKeyLookupForSagasCorrelatedByProperties();

            configuration.UsePersistence<AzureTablePersistence, StorageType.Outbox>();

            if (endpointName != Conventions.EndpointNamingConvention(typeof(When_saga_started_concurrently.ConcurrentHandlerEndpoint)))
            {
                configuration.Recoverability().Immediate(c => c.NumberOfRetries(1));
            }

            configuration.Pipeline.Register(new PartitionKeyProviderBehavior.PartitionKeyProviderBehaviorRegisterStep());

            return Task.FromResult(0);
        }

        Task IConfigureEndpointTestExecution.Cleanup() => Task.FromResult(0);

        class PartitionKeyProviderBehavior : Behavior<IIncomingLogicalMessageContext>
        {
            readonly ScenarioContext scenarioContext;
            readonly IReadOnlySettings settings;

            public PartitionKeyProviderBehavior(ScenarioContext scenarioContext, IReadOnlySettings settings)
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
                    provider => new PartitionKeyProviderBehavior(provider.GetRequiredService<ScenarioContext>(), provider.GetRequiredService<IReadOnlySettings>())) =>
                    InsertBeforeIfExists(nameof(LogicalOutboxBehavior));
            }
        }
    }
}