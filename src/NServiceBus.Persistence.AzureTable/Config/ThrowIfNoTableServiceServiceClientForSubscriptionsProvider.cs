﻿namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using Azure.Data.Tables;

    sealed class ThrowIfNoTableServiceServiceClientForSubscriptionsProvider : IProvideTableServiceClientForSubscriptions
    {
        public TableServiceClient Client => throw new Exception(
            $"No TableServiceClient has been configured. Either use `persistence.UseTableServiceClient(client)`, register an implementation of `{nameof(IProvideTableServiceClient)}` in the container or provide a connection string.");
    }
}