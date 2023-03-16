namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using Azure.Data.Tables;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.AcceptanceTests.Sagas;
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
                         .UseTableServiceClient(tableServiceClient);

            configuration.UsePersistence<AzureTablePersistence, StorageType.Outbox>();

            if (endpointName != Conventions.EndpointNamingConvention(typeof(When_saga_started_concurrently.ConcurrentHandlerEndpoint)))
            {
                configuration.Recoverability().Immediate(c => c.NumberOfRetries(1));
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

        Task IConfigureEndpointTestExecution.Cleanup() => Task.FromResult(0);

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
}