using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Azure.Transports.WindowsAzureStorageQueues;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Transports;

public class ConfigureEndpointAzureStorageQueueTransport : IConfigureEndpointTestExecution
{
    public async Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings)
    {
        var connectionString = settings.Get<string>("Transport.ConnectionString");
        //connectionString = "UseDevelopmentStorage=true;";
        configuration.UseSerialization<XmlSerializer>();
        configuration.UseTransport<AzureStorageQueueTransport>()
            .ConnectionString(connectionString)
            .MessageInvisibleTime(TimeSpan.FromSeconds(5))
            .SerializeMessageWrapperWith(definition => MessageWrapperSerializer.Xml.Value);

        await CleanQueuesUsedByTest(connectionString, configuration);
    }

    public Task Cleanup()
    {
        return Task.FromResult(0);
    }

    private static async Task CleanQueuesUsedByTest(string connectionString, EndpointConfiguration configuration)
    {
        var storage = CloudStorageAccount.Parse(connectionString);
        var queues = storage.CreateCloudQueueClient();

        var queuesNames = GetTestRelatedQueueNames(configuration);

        foreach (var queuesName in queuesNames)
        {
            var queue = queues.GetQueueReference(queuesName);
            if (await queue.ExistsAsync())
            {
                await queue.ClearAsync();
            }
        }
    }

    private static IEnumerable<string> GetTestRelatedQueueNames(ExposeSettings configuration)
    {
        var bindings = configuration.GetSettings().Get<QueueBindings>();
        var generator = new QueueAddressGenerator(configuration.GetSettings());
        return bindings.ReceivingAddresses.Concat(bindings.SendingAddresses).Select(queue => generator.GetQueueName(queue));
    }
}