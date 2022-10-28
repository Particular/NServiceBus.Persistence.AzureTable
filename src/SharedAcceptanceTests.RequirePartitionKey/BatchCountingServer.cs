namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;
    using Azure.Core;
    using Azure.Data.Tables;
    using NServiceBus.AcceptanceTests.EndpointTemplates;

    class BatchCountingServer : ServerWithNoDefaultPersistenceDefinitions
    {
        public TransactionalBatchCounterPolicy TransactionalBatchCounterPolicy { get; } = new TransactionalBatchCounterPolicy();

        public override Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, Func<EndpointConfiguration, Task> configurationBuilderCustomization) =>
            base.GetConfiguration(runDescriptor, endpointConfiguration, async configuration =>
            {
                var tableClientOptions = new TableClientOptions();
                tableClientOptions.AddPolicy(TransactionalBatchCounterPolicy, HttpPipelinePosition.PerCall);

                var tableServiceClient = new TableServiceClient(SetupFixture.ConnectionString, tableClientOptions);
                var persistenceConfiguration = new TestSuiteConstraints(tableServiceClient).CreatePersistenceConfiguration();

                await configuration.DefinePersistence(persistenceConfiguration, runDescriptor, endpointConfiguration);
                await configurationBuilderCustomization(configuration);
            });
    }
}
