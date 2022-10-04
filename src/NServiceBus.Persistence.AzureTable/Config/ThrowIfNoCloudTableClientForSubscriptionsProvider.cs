namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using Azure.Data.Tables;

    class ThrowIfNoCloudTableClientForSubscriptionsProvider : IProvideCloudTableClientForSubscriptions
    {
        // TODO: adjust
        public TableServiceClient Client => throw new Exception(
            $"No CloudTableClient has been configured. Either use `persistence.UseCloudTableClient(client)`, register an implementation of `{nameof(IProvideCloudTableClient)}` in the container or provide a connection string.");
    }
}