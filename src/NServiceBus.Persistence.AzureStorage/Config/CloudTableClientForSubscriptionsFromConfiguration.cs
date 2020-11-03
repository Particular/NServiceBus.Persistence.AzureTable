﻿namespace NServiceBus.Persistence.AzureStorage
{
    using Microsoft.Azure.Cosmos.Table;

    class CloudTableClientForSubscriptionsFromConfiguration : IProvideCloudTableClientForSubscriptions
    {
        public CloudTableClientForSubscriptionsFromConfiguration(CloudTableClient client)
        {
            Client = client;
        }

        public CloudTableClient Client { get; }
    }
}