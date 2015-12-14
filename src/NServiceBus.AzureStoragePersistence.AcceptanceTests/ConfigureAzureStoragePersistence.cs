using System;
using NServiceBus;
using NServiceBus.Persistence;
using NServiceBus.SagaPersisters;

public class ConfigureAzureStoragePersistence
{
    public void Configure(BusConfiguration config)
    {
        var connectionString = Environment.GetEnvironmentVariable("AzureStoragePersistence.ConnectionString");
        config.UsePersistence<AzureStoragePersistence, StorageType.Subscriptions>().ConnectionString(connectionString);
        config.UsePersistence<AzureStoragePersistence, StorageType.Sagas>().ConnectionString(connectionString);
        config.UsePersistence<AzureStoragePersistence, StorageType.Timeouts>().ConnectionString(connectionString);
    }

    public void Cleanup()
    {
    }
}
