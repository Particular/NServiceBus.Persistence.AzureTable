namespace NServiceBus.Persistence.AzureStorage
{
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    ///
    /// </summary>
    public interface IProvideCloudTableClient
    {
        /// <summary>
        ///
        /// </summary>
        CloudTableClient Client { get; }
    }
}