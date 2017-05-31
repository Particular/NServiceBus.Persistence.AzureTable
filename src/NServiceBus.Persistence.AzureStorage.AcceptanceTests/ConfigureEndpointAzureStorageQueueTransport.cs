using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.AcceptanceTests.ScenarioDescriptors;

public class ConfigureEndpointAzureStorageQueueTransport : IConfigureEndpointTestExecution
{
    static string ConnectionString => EnvironmentHelper.GetEnvironmentVariable($"{nameof(AzureStorageQueueTransport)}.ConnectionString") ?? "UseDevelopmentStorage=true";

    public async Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        var connectionString = ConnectionString;

        var transportRouting = configuration.UseTransport<AzureStorageQueueTransport>()
            .Transactions(TransportTransactionMode.ReceiveOnly)
            .ConnectionString(connectionString)
            .Routing();


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

    static async Task CleanQueuesUsedByTest(string connectionString)
    {
        var storage = CloudStorageAccount.Parse(connectionString);
        var client = storage.CreateCloudQueueClient();
        var queues = GetTestRelatedQueues(client).ToArray();

        var clearTask = Task.WhenAll(queues.Select(q => q.ClearAsync()));
        var timeoutTask = Task.Delay(TimeSpan.FromMinutes(1));
        var result = await Task.WhenAny(clearTask, timeoutTask);

        if (result == timeoutTask)
        {
            throw new TimeoutException("Waiting for cleaning queues took too long.");
        }

        // await to get the exception in case something went wrong when clearing the queues.
        await result;
    }

    static IEnumerable<CloudQueue> GetTestRelatedQueues(CloudQueueClient queues)
    {
        // for now, return all
        return queues.ListQueues();
    }
}