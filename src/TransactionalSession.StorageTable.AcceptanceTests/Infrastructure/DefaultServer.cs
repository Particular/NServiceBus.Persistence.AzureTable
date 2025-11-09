namespace NServiceBus.TransactionalSession.AcceptanceTests;

using System;
using System.IO;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using AcceptanceTesting.Support;
using Configuration.AdvancedExtensibility;
using NUnit.Framework;
using Pipeline;
using Settings;

public class DefaultServer : IEndpointSetupTemplate
{
    public virtual async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointCustomizations,
        Func<EndpointConfiguration, Task> configurationBuilderCustomization)
    {
        var endpointConfiguration = new EndpointConfiguration(endpointCustomizations.EndpointName);

        endpointConfiguration.EnableInstallers();
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();

        endpointConfiguration.Recoverability()
            .Delayed(delayed => delayed.NumberOfRetries(0))
            .Immediate(immediate => immediate.NumberOfRetries(0));
        endpointConfiguration.SendFailedMessagesTo("error");

        var storageDir = Path.Combine(Path.GetTempPath(), "learn", TestContext.CurrentContext.Test.ID);

        endpointConfiguration.UseTransport(new AcceptanceTestingTransport { StorageLocation = storageDir });

        var persistence = endpointConfiguration.UsePersistence<AzureTablePersistence>();
        persistence.EnableTransactionalSession();
        persistence.UseTableServiceClient(SetupFixture.TableServiceClient);
        persistence.DefaultTable(SetupFixture.TableName);

        endpointConfiguration.GetSettings().Set(persistence);
        // This populates the partition key at the physical stage to test the conventional outbox use-case
        endpointConfiguration.Pipeline.Register(typeof(PartitionKeyProviderBehavior), "Populates the partition key");

        if (runDescriptor.ScenarioContext is TransactionalSessionTestContext testContext)
        {
            endpointConfiguration.RegisterStartupTask(sp => new CaptureServiceProviderStartupTask(sp, testContext, endpointCustomizations.EndpointName));
        }

        await configurationBuilderCustomization(endpointConfiguration).ConfigureAwait(false);

        // scan types at the end so that all types used by the configuration have been loaded into the AppDomain
        endpointConfiguration.ScanTypesForTest(endpointCustomizations);

        return endpointConfiguration;
    }

    class PartitionKeyProviderBehavior : Behavior<ITransportReceiveContext>
    {
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

        readonly ScenarioContext scenarioContext;
        readonly IReadOnlySettings settings;
    }
}