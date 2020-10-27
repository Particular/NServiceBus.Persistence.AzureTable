using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.Sagas;
using NServiceBus.Persistence.AzureStorage;
using NServiceBus.Persistence.AzureStorage.Testing;
using NServiceBus.Pipeline;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

public class ConfigureEndpointAzureStoragePersistence : IConfigureEndpointTestExecution
{
    static string ConnectionString => Utilities.GetEnvConfiguredConnectionStringForPersistence();

    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        var persistence = configuration.UsePersistence<AzureStoragePersistence>();
        persistence.ConnectionString(ConnectionString);

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
        ScenarioContext scenarioContext;

        public PartitionKeyProviderBehavior(ScenarioContext scenarioContext)
        {
            this.scenarioContext = scenarioContext;
        }

        public override Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
        {
            context.Extensions.Set(new TableEntityPartitionKey(scenarioContext.TestRunId.ToString()));
            context.Extensions.Set(new TableInformation(SetupFixture.TableName));
            return next();
        }

        public class PartitionKeyProviderBehaviorRegisterStep : RegisterStep
        {
            public PartitionKeyProviderBehaviorRegisterStep() : base(nameof(PartitionKeyProviderBehavior),
                typeof(PartitionKeyProviderBehavior),
                "Populates the partition key",
                provider => new PartitionKeyProviderBehavior(provider.GetRequiredService<ScenarioContext>()))
            {
                InsertBeforeIfExists(nameof(LogicalOutboxBehavior));
            }
        }
    }
}