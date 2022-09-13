namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using AcceptanceTesting.Support;
    using NUnit.Framework;
    using Pipeline;
    using Settings;

    public class TransactionSessionDefaultServer : IEndpointSetupTemplate
    {
        public virtual Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration,
            Action<EndpointConfiguration> configurationBuilderCustomization)
        {
            var builder = new EndpointConfiguration(endpointConfiguration.EndpointName);
            builder.EnableInstallers();

            builder.Recoverability()
                .Delayed(delayed => delayed.NumberOfRetries(0))
                .Immediate(immediate => immediate.NumberOfRetries(0));
            builder.SendFailedMessagesTo("error");

            var storageDir = Path.Combine(Path.GetTempPath(), "learn", TestContext.CurrentContext.Test.ID);

            var transport = builder.UseTransport<AcceptanceTestingTransport>();
            transport.StorageDirectory(storageDir);

            var persistence = builder.UsePersistence<AzureTablePersistence>();
            persistence.EnableTransactionalSession();
            persistence.UseCloudTableClient(SetupFixture.TableClient);
            persistence.DefaultTable(SetupFixture.TableName);

            // This populates the partition key at the physical stage to test the conventional outbox use-case
            builder.Pipeline.Register(typeof(PartitionKeyProviderBehavior), "Populates the partition key");

            builder.RegisterComponents(c => c.RegisterSingleton(runDescriptor.ScenarioContext)); // register base ScenarioContext type
            builder.RegisterComponents(c => c.RegisterSingleton(runDescriptor.ScenarioContext.GetType(), runDescriptor.ScenarioContext)); // register specific implementation

            endpointConfiguration.TypesToInclude.Add(typeof(CaptureBuilderFeature)); // required because the test assembly is excluded from scanning by default
            builder.EnableFeature<CaptureBuilderFeature>();

            configurationBuilderCustomization(builder);

            // scan types at the end so that all types used by the configuration have been loaded into the AppDomain
            builder.TypesToIncludeInScan(endpointConfiguration.GetTypesScopedByTestClass());

            return Task.FromResult(builder);
        }

        class PartitionKeyProviderBehavior : Behavior<ITransportReceiveContext>
        {

            public PartitionKeyProviderBehavior(ScenarioContext scenarioContext, ReadOnlySettings settings)
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
            readonly ReadOnlySettings settings;
        }
    }
}