using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;

public class ConfigureEndpointAzureStorageQueueTransport : IConfigureEndpointTestExecution
{
    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        var connectionString = ConfigureEndpointAzureStoragePersistence.GetConnectionString();
        //connectionString = "UseDevelopmentStorage=true;";

        var transportRouting = configuration.UseTransport<AzureStorageQueueTransport>()
            .Transactions(TransportTransactionMode.ReceiveOnly)
            .ConnectionString(connectionString)
            .MessageInvisibleTime(TimeSpan.FromSeconds(5))
            .Routing();

        //configuration.UseSerialization<XmlSerializer>();

        foreach (var publisher in publisherMetadata.Publishers)
        {
            foreach (var @event in publisher.Events)
            {
                transportRouting.RegisterPublisher(@event, publisher.PublisherName);
            }
        }


        configuration.RegisterComponents(c => { c.ConfigureComponent<TestIndependenceMutator>(DependencyLifecycle.SingleInstance); });
        configuration.Pipeline.Register("TestIndependenceBehavior", typeof(TestIndependenceSkipBehavior), "Skips messages not created during the current test.");

        return Task.FromResult(0);
    }

    public Task Cleanup()
    {
        return Task.FromResult(0);
    }
}