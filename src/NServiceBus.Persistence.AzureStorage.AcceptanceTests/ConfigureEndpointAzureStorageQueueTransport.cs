using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;

public class ConfigureEndpointAzureStorageQueueTransport : IConfigureEndpointTestExecution
{
    public async Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
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

        await CleanQueuesUsedByTest(connectionString);
    }

    public Task Cleanup()
    {
        return Task.FromResult(0);
    }

    static Task CleanQueuesUsedByTest(string connectionString)
    {
        var storage = CloudStorageAccount.Parse(connectionString);
        var client = storage.CreateCloudQueueClient();
        var queues = GetTestRelatedQueues(client).ToArray();

        var tasks = new Task[queues.Length];
        for (var i = 0; i < queues.Length; i++)
        {
            tasks[i] = queues[i].ClearAsync();

        }

        return Task.WhenAll(tasks);
    }

    static IEnumerable<CloudQueue> GetTestRelatedQueues(CloudQueueClient queues)
    {
        // for now, return all
        return queues.ListQueues();
    }
}