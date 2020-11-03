﻿namespace NServiceBus.Persistence.AzureStorage
{
    using Microsoft.Azure.Cosmos.Table;

    class CloudTableClientFromConfiguration : IProvideCloudTableClient
    {
        public CloudTableClientFromConfiguration(CloudTableClient client)
        {
            Client = client;
        }

        public CloudTableClient Client { get; }
    }
}