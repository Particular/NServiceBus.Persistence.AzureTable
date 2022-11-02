namespace NServiceBus.Persistence.AzureTable
{
    /// <summary>
    /// Provides a CloudTableClient for the saga and outbox storage type via dependency injection. A custom implementation can be registered on the container and will be picked up by the persistence.
    /// </summary>
    [ObsoleteEx(Message = "The IProvideCloudTableClient is deprecated in favor of IProvideTableServiceClient, which supports the TableServiceClient API provided by Azure.Data.Tables.", TreatAsErrorFromVersion = "5.0.0", RemoveInVersion = "6.0.0")]
    public interface IProvideCloudTableClient
    {
        /// <summary>
        /// The CloudTableClient to use.
        /// </summary>
        object Client { get; }
    }

    /// <summary>
    /// Provides a CloudTableClient for the subscription storage type via dependency injection. A custom implementation can be registered on the container and will be picked up by the persistence.
    /// </summary>
    [ObsoleteEx(Message = "The IProvideCloudTableClientForSubscriptions is deprecated in favor of IProvideTableServiceClientForSubscriptions, which supports the TableServiceClient API provided by Azure.Data.Tables.", TreatAsErrorFromVersion = "5.0.0", RemoveInVersion = "6.0.0")]
    public interface IProvideCloudTableClientForSubscriptions
    {
        /// <summary>
        /// The CloudTableClient to use.
        /// </summary>
        object Client { get; }
    }
}

namespace NServiceBus
{
    using System;

    public static partial class ConfigureAzureSagaStorage
    {
        /// <summary>
        /// Cloud Table Client to use for Saga, Outbox and Subscription storage.
        /// </summary>
        [ObsoleteEx(Message = "The UseCloudTableClient method is deprecated in favor of the UseTableServiceClient method, which supports the TableServiceClient API provided by Azure.Data.Tables.", TreatAsErrorFromVersion = "5.0.0", RemoveInVersion = "6.0.0")]
        public static PersistenceExtensions<AzureTablePersistence> UseCloudTableClient(
            this PersistenceExtensions<AzureTablePersistence> config, object client)
        {
            throw new NotImplementedException();
        }
    }

    public static partial class ConfigureAzureSubscriptionStorage
    {
        /// <summary>
        /// Cloud Table Client to use for the Subscription storage.
        /// </summary>
        [ObsoleteEx(Message = "The UseCloudTableClient method is deprecated in favor of the UseTableServiceClient method, which supports the TableServiceClient API provided by Azure.Data.Tables.", TreatAsErrorFromVersion = "5.0.0", RemoveInVersion = "6.0.0")]
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Subscriptions> UseCloudTableClient(
            this PersistenceExtensions<AzureTablePersistence, StorageType.Subscriptions> config,
            object client)
        {
            throw new NotImplementedException();
        }
    }
}