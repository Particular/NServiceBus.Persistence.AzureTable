using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.Routing.MessageDrivenSubscriptions;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

public class ConfigureEndpointAzureTablePersistence : IConfigureEndpointTestExecution
{
    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        var subscriptionPersistence = configuration.UsePersistence<AzureTablePersistence, StorageType.Subscriptions>();
        subscriptionPersistence.DisableTableCreation();
        subscriptionPersistence.UseCloudTableClient(SetupFixture.TableClient);
        subscriptionPersistence.DefaultTable(SetupFixture.TableName);

        var recoverabilitySettings = configuration.Recoverability();

        if (endpointName != Conventions.EndpointNamingConvention(typeof(MultiSubscribeToPolymorphicEvent.Publisher1))
            || endpointName != Conventions.EndpointNamingConvention(typeof(MultiSubscribeToPolymorphicEvent.Publisher2))
            || endpointName != Conventions.EndpointNamingConvention(typeof(MultiSubscribeToPolymorphicEvent.Publisher2)))
        {
            recoverabilitySettings.Immediate(c => c.NumberOfRetries(1));
        }

        return Task.FromResult(0);
    }

    Task IConfigureEndpointTestExecution.Cleanup()
    {
        return Task.FromResult(0);
    }
}