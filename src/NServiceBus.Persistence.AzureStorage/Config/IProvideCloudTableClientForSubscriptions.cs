using Microsoft.Azure.Cosmos.Table;

namespace NServiceBus
{
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