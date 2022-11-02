﻿namespace NServiceBus.Persistence.AzureTable
{
    using Azure.Data.Tables;

    /// <summary>
    /// Provides a CloudTableClient for the subscription storage type via dependency injection. A custom implementation can be registered on the container and will be picked up by the persistence.
    /// </summary>
    public interface IProvideCloudTableClientForSubscriptions
    {
        /// <summary>
        /// The CloudTableClient to use.
        /// </summary>
        TableServiceClient Client { get; }
    }
}