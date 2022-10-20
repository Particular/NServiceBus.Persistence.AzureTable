﻿namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using Azure.Data.Tables;

    class ThrowIfNoCloudTableClientProvider : IProvideCloudTableClient
    {
        // TODO: adjust
        public TableServiceClient Client => throw new Exception($"No CloudTableClient has been configured. Either use `persistence.UseTableServiceClient(client)`, register an implementation of `{nameof(IProvideCloudTableClient)}` in the container or provide a connection string.");
    }
}