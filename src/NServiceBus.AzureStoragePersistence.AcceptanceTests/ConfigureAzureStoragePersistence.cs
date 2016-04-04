using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Persistence;
using NServiceBus.SagaPersisters;

public class ConfigureEndpointAzureStoragePersistence : IConfigureEndpointTestExecution
{
    public Task Configure(string endpointName, EndpointConfiguration config, RunSettings settings)
    {
        var connectionString = Environment.GetEnvironmentVariable("AzureStoragePersistence.ConnectionString");
        config.UsePersistence<AzureStoragePersistence, StorageType.Subscriptions>().ConnectionString(connectionString);
        config.UsePersistence<AzureStoragePersistence, StorageType.Sagas>().ConnectionString(connectionString);
        config.UsePersistence<AzureStoragePersistence, StorageType.Timeouts>().ConnectionString(connectionString);

        return Task.FromResult(0);
    }

    Task IConfigureEndpointTestExecution.Cleanup()
    {
        return Task.FromResult(0);
    }
}