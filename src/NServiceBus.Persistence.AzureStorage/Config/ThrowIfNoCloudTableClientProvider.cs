namespace NServiceBus.Persistence.AzureStorage.Config
{
    using System;
    using Microsoft.Azure.Cosmos.Table;

    class ThrowIfNoCloudTableClientProvider : IProvideCloudTableClient
    {
        public CloudTableClient Client => throw new Exception($"No CloudTableClient has been configured. Either provide a connection string, use `persistence.UseCloudTableClient(client)` or register an implementation of `{nameof(IProvideCloudTableClient)}` in the container.");
    }
}