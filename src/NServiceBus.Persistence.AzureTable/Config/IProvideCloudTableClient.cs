namespace NServiceBus.Persistence.AzureTable
{
    using Azure.Data.Tables;

    /// <summary>
    /// Provides a CloudTableClient for the saga and outbox storage type via dependency injection. A custom implementation can be registered on the container and will be picked up by the persistence.
    /// </summary>
    public interface IProvideCloudTableClient
    {
        /// <summary>
        /// The CloudTableClient to use.
        /// </summary>
        TableServiceClient Client { get; }
    }
}