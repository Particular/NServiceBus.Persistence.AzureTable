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
        public ConfigureAzureTablePersistence(TableServiceClient tableServiceClient = null) =>
            this.tableServiceClient = tableServiceClient ?? SetupFixture.TableServiceClient;

        Task IConfigureEndpointTestExecution.Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
        {
            var sagaPersistence = configuration.UsePersistence<AzureTablePersistence, StorageType.Sagas>()
                .UseTableServiceClient(tableServiceClient);

            if (!(settings.TryGet("allowTableCreation", out bool allowTableCreation) && allowTableCreation))
            {
                sagaPersistence.DisableTableCreation();
            }


            configuration.UsePersistence<AzureTablePersistence, StorageType.Outbox>();

            if (endpointName != Conventions.EndpointNamingConvention(typeof(When_saga_started_concurrently.ConcurrentHandlerEndpoint)))
            {
                configuration.Recoverability().Immediate(c => c.NumberOfRetries(1));
            }

            if (!settings.TryGet<DoNotRegisterDefaultPartitionKeyProvider>(out _))
            {
                configuration.Pipeline.Register(new PartitionKeyProviderBehavior.Registration());
            }

            if (settings.TryGet<TableNameProvider>(out var tableNameProvider))
            {
                configuration.Pipeline.Register(new TableInformationProviderBehavior.Registration(tableNameProvider.GetTableName));
            }
            else if (!settings.TryGet<DoNotRegisterDefaultTableNameProvider>(out _))
            {
                configuration.Pipeline.Register(new TableInformationProviderBehavior.Registration());
            }

            return Task.CompletedTask;
        }

        Task IConfigureEndpointTestExecution.Cleanup() => Task.CompletedTask;

        readonly TableServiceClient tableServiceClient;

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
                    provider => new PartitionKeyProviderBehavior(provider.GetRequiredService<ScenarioContext>())) =>
                    InsertBeforeIfExists(nameof(LogicalOutboxBehavior));
            }
        }

        class TableInformationProviderBehavior : Behavior<IIncomingLogicalMessageContext>
        {
            readonly IReadOnlySettings settings;
            readonly Func<IIncomingLogicalMessageContext, string> tableNameProvider;

            public TableInformationProviderBehavior(IReadOnlySettings settings, Func<string> tableNameProvider)
            {
                this.settings = settings;

                this.tableNameProvider = tableNameProvider == null
                    ? DefaultTableNameProvider
                    : context => tableNameProvider();
            }

            string DefaultTableNameProvider(IIncomingLogicalMessageContext context)
            {
                if (!settings.TryGet<TableInformation>(out _) && !context.Extensions.TryGet<TableInformation>(out _))
                {
                    return SetupFixture.TableName;
                }
                else
                {
                    return null;
                }
            }

            public override Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
            {
                var tableName = tableNameProvider(context);

                if (!string.IsNullOrEmpty(tableName))
                {
                    context.Extensions.Set(new TableInformation(tableName));
                }

                return next();
            }

            public class Registration : RegisterStep
            {
                public Registration(Func<string> tableNameProvider = null)
                    : base(
                          nameof(TableInformationProviderBehavior),
                          typeof(TableInformationProviderBehavior),
                          "Populates the table information",
                          serviceProvider => new TableInformationProviderBehavior(
                              serviceProvider.GetRequiredService<IReadOnlySettings>(),
                              tableNameProvider))
                    => InsertBeforeIfExists(nameof(LogicalOutboxBehavior));
            }
        }
    }
}