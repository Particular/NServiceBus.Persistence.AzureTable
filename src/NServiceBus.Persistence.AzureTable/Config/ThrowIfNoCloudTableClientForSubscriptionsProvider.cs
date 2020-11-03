namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using Microsoft.Azure.Cosmos.Table;

    class ThrowIfNoCloudTableClientForSubscriptionsProvider : IProvideCloudTableClientForSubscriptions
    {
        public CloudTableClient Client => throw new Exception(
            $"No CloudTableClient has been configured. Either use `persistence.UseCloudTableClient(client)`, register an implementation of `{nameof(IProvideCloudTableClient)}` in the container or provide a connection string.");
    }
}