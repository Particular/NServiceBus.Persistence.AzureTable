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
    public async Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings)
    {
        var connectionString = settings.Get<string>("Transport.ConnectionString");
        //connectionString = "UseDevelopmentStorage=true;";

        configuration.UseTransport<AzureStorageQueueTransport>()
            .Transactions(TransportTransactionMode.ReceiveOnly)
            .ConnectionString(connectionString)
            .MessageInvisibleTime(TimeSpan.FromSeconds(5));

        //configuration.UseSerialization<XmlSerializer>();

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