namespace NServiceBus.Persistence.AzureStorage
{
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// Provides a CloudTableClient for the saga and outbox storage type via dependency injection. A custom implementation can be registered on the container and will be picked up by the persistence.
    /// </summary>
    public interface IProvideCloudTableClient
    {
        /// <summary>
        /// The CloudTableClient to use.
        /// </summary>
        CloudTableClient Client { get; }
    }
}