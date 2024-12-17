namespace NServiceBus.AcceptanceTests
{
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.AcceptanceTests.Sagas;
    using Conventions = AcceptanceTesting.Customization.Conventions;

    public class ConfigureEndpointAzureStoragePersistence : IConfigureEndpointTestExecution
    {
        Task IConfigureEndpointTestExecution.Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
        {
            var sagaPersistence = configuration.UsePersistence<AzureTablePersistence, StorageType.Sagas>()
                .UseTableServiceClient(SetupFixture.TableServiceClient);

            sagaPersistence.DefaultTable(SetupFixture.TableName);

            if (endpointName != Conventions.EndpointNamingConvention(typeof(When_saga_started_concurrently.ConcurrentHandlerEndpoint)))
            {
                configuration.Recoverability().Immediate(c => c.NumberOfRetries(1));
            }

            return Task.FromResult(0);
        }

        Task IConfigureEndpointTestExecution.Cleanup() => Task.FromResult(0);
    }
}