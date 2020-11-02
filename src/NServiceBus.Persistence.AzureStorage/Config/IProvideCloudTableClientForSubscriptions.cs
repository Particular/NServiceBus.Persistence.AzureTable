namespace NServiceBus.Persistence.AzureStorage
{
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    ///
    /// </summary>
    public interface IProvideCloudTableClientForSubscriptions
    {
        /// <summary>
        ///
        /// </summary>
        CloudTableClient Client { get; }
    }
}