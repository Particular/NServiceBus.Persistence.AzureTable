namespace NServiceBus
{
    using Features;
    using Persistence;
    using Persistence.AzureStorage;

    /// <summary></summary>
    public class AzureStoragePersistence : PersistenceDefinition
    {
        AzureStoragePersistence()
        {
            Supports<StorageType.Sagas>(s => s.EnableFeatureByDefault<AzureStorageSagaPersistence>());
            Supports<StorageType.Outbox>(s => s.EnableFeatureByDefault<OutboxStorage>());
            Supports<StorageType.Subscriptions>(s => s.EnableFeatureByDefault<AzureStorageSubscriptionPersistence>());
        }
    }
}