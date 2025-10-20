namespace NServiceBus
{
    using Persistence;
    using Persistence.AzureTable;

    /// <summary></summary>
    public sealed class AzureTablePersistence : PersistenceDefinition, IPersistenceDefinitionFactory<AzureTablePersistence>
    {
        AzureTablePersistence()
        {
            Supports<StorageType.Sagas, SagaStorage>();
            Supports<StorageType.Outbox, OutboxStorage>();
            Supports<StorageType.Subscriptions, SubscriptionStorage>();
        }

        static AzureTablePersistence IPersistenceDefinitionFactory<AzureTablePersistence>.Create() => new();
    }
}