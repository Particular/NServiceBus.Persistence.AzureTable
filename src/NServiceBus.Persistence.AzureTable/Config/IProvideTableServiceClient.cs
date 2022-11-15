namespace NServiceBus.Persistence.AzureTable
{
    using Azure.Data.Tables;

    /// <summary>
    /// Provides a <see cref="Azure.Data.Tables.TableServiceClient"/> for the saga and outbox storage type via dependency injection. A custom implementation can be registered on the container and will be picked up by the persistence.
    /// </summary>
    public interface IProvideTableServiceClient
    {
        /// <summary>
        /// The <see cref="Azure.Data.Tables.TableServiceClient"/> to use.
        /// </summary>
        TableServiceClient Client { get; }
    }
}