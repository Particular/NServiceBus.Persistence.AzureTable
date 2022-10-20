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
        public virtual async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration,
            Func<EndpointConfiguration, Task> configurationBuilderCustomization)
        {
            var builder = new EndpointConfiguration(endpointConfiguration.EndpointName);
            builder.EnableInstallers();

            builder.Recoverability()
                .Delayed(delayed => delayed.NumberOfRetries(0))
                .Immediate(immediate => immediate.NumberOfRetries(0));
            builder.SendFailedMessagesTo("error");

            var storageDir = Path.Combine(Path.GetTempPath(), "learn", TestContext.CurrentContext.Test.ID);

            builder.UseTransport(new AcceptanceTestingTransport
            {
                StorageLocation = storageDir
            });

            var persistence = builder.UsePersistence<AzureTablePersistence>();
            persistence.EnableTransactionalSession();
            persistence.UseTableServiceClient(SetupFixture.TableClient);
            persistence.DefaultTable(SetupFixture.TableName);

            // This populates the partition key at the physical stage to test the conventional outbox use-case
            builder.Pipeline.Register(typeof(PartitionKeyProviderBehavior), "Populates the partition key");

            builder.RegisterStartupTask(sp => new CaptureServiceProviderStartupTask(sp, runDescriptor.ScenarioContext));

            await configurationBuilderCustomization(builder).ConfigureAwait(false);

            // scan types at the end so that all types used by the configuration have been loaded into the AppDomain
            builder.TypesToIncludeInScan(endpointConfiguration.GetTypesScopedByTestClass());

            return builder;
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
}