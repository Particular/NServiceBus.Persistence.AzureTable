namespace NServiceBus.Persistence.AzureTable
{
    static class WellKnownConfigurationKeys
    {
        public const string SagaStorageAssumeSecondaryIndicesExist = "AzureSagaStorage.SagaStorageAssumeSecondaryIndicesExist";
        public const string SagaStorageAssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey = "AzureSagaStorage.SagaStorageAssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey";
        public const string SagaStorageCompatibilityMode = "AzureSagaStorage.EnableCompatibilityMode";
        public const string SagaStorageConventionalTablePrefix = "AzureSagaStorage.ConventionalTablePrefix";
        public const string SubscriptionStorageTableName = "AzureSubscriptionStorage.TableName";
        public const string SubscriptionStorageCacheFor = "AzureSubscriptionStorage.CacheFor";
    }
}