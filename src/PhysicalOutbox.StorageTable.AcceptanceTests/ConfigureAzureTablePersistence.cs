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
            if (endpointName != Conventions.EndpointNamingConvention(typeof(When_using_outbox_control_message.Endpoint)))
            {
                configuration.Pipeline.Register(typeof(PartitionKeyProviderBehavior), "Populates the partition key");
            }
            return Task.FromResult(0);
        }

        Task IConfigureEndpointTestExecution.Cleanup() => Task.FromResult(0);

        class PartitionKeyProviderBehavior : Behavior<ITransportReceiveContext>
        {
            readonly ScenarioContext scenarioContext;
            readonly IReadOnlySettings settings;

            public PartitionKeyProviderBehavior(ScenarioContext scenarioContext, IReadOnlySettings settings)
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
}