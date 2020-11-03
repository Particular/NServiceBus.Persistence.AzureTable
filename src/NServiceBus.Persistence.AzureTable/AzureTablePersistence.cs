namespace NServiceBus
{
    using Features;
    using Persistence;
    using Persistence.AzureTable;

    /// <summary></summary>
    public sealed class AzureTablePersistence : PersistenceDefinition
    {
        internal AzureTablePersistence()
        {
            Supports<StorageType.Sagas>(s => s.EnableFeatureByDefault<SagaStorage>());
            Supports<StorageType.Outbox>(s => s.EnableFeatureByDefault<OutboxStorage>());
            Supports<StorageType.Subscriptions>(s => s.EnableFeatureByDefault<SubscriptionStorage>());
        }
    }
}