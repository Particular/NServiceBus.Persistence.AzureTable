using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Pipeline;
using NServiceBus.Settings;
using NServiceBus.TransactionalSession;
using NServiceBus.TransactionalSession.AcceptanceTests;

public class ConfigureAzureTablePersistence : IConfigureEndpointTestExecution
{
    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        var sagaPersistence = configuration.UsePersistence<AzureTablePersistence, StorageType.Sagas>();
        sagaPersistence.DisableTableCreation();
        sagaPersistence.UseCloudTableClient(SetupFixture.TableClient);

        sagaPersistence.Compatibility().DisableSecondaryKeyLookupForSagasCorrelatedByProperties();

        var outboxPersistence = configuration.UsePersistence<AzureTablePersistence, StorageType.Outbox>();
        outboxPersistence.EnableTransactionalSession();

        // This populates the partition key at the physical stage to test the conventional outbox use-case
        configuration.Pipeline.Register(typeof(PartitionKeyProviderBehavior), "Populates the partition key");

        return Task.CompletedTask;
    }

    Task IConfigureEndpointTestExecution.Cleanup() => Task.CompletedTask;

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