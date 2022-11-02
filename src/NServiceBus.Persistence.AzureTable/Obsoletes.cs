namespace NServiceBus.Persistence.AzureTable
{
    /// <summary>
    /// Provides a CloudTableClient for the saga and outbox storage type via dependency injection. A custom implementation can be registered on the container and will be picked up by the persistence.
    /// </summary>
    [ObsoleteEx(TreatAsErrorFromVersion = "5.0.0", RemoveInVersion = "6.0.0")]
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
    [ObsoleteEx(TreatAsErrorFromVersion = "5.0.0", RemoveInVersion = "6.0.0")]
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
        [ObsoleteEx(TreatAsErrorFromVersion = "5.0.0", RemoveInVersion = "6.0.0")]
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
        [ObsoleteEx(TreatAsErrorFromVersion = "5.0.0", RemoveInVersion = "6.0.0")]
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Subscriptions> UseCloudTableClient(
            this PersistenceExtensions<AzureTablePersistence, StorageType.Subscriptions> config,
            object client)
        {
            throw new NotImplementedException();
        }
    }
}