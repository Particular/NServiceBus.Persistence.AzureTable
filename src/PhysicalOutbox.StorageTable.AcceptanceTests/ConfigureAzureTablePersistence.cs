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
            var sagaPersistence = configuration.UsePersistence<AzureTablePersistence, StorageType.Sagas>()
                .UseTableServiceClient(tableServiceClient);

            var outboxPersistence = configuration.UsePersistence<AzureTablePersistence, StorageType.Outbox>();

            if (!(settings.TryGet("allowTableCreation", out bool allowTableCreation) && allowTableCreation))
            {
                sagaPersistence.DisableTableCreation();
                outboxPersistence.DisableTableCreation();
            }

            if (endpointName != Conventions.EndpointNamingConvention(typeof(When_saga_started_concurrently.ConcurrentHandlerEndpoint)))
            {
                configuration.Recoverability().Immediate(c => c.NumberOfRetries(1));
            }

            // This populates the partition key at the physical stage to test the conventional outbox use-case
            if (!settings.TryGet<DoNotRegisterDefaultPartitionKeyProvider>(out _))
            {
                configuration.Pipeline.Register(typeof(PartitionKeyProviderBehavior), "Populates the partition key");
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
            readonly IReadOnlySettings settings;
            readonly Func<ITransportReceiveContext, string> tableNameProvider;

            public TableInformationProviderBehavior(IReadOnlySettings settings, Func<string> tableNameProvider)
            {
                this.settings = settings;

                this.tableNameProvider = tableNameProvider == null
                    ? DefaultTableNameProvider
                    : context => tableNameProvider();
            }

            string DefaultTableNameProvider(ITransportReceiveContext context)
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

            public override Task Invoke(ITransportReceiveContext context, Func<Task> next)
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
                public Registration(Func<string> tableNameProvider = null) : base(nameof(TableInformationProviderBehavior), typeof(TableInformationProviderBehavior), "Populates the table information", serviceProvider => new TableInformationProviderBehavior(serviceProvider.GetRequiredService<IReadOnlySettings>(), tableNameProvider))
                {
                }
            }
        }
    }
}