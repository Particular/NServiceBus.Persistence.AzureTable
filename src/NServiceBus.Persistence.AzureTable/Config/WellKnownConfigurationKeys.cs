namespace NServiceBus.Persistence.AzureTable
{
    static class WellKnownConfigurationKeys
    {
        public const string SagaStorageCreateSchema = "AzureSagaStorage.CreateSchema";
        public const string SagaStorageAssumeSecondaryIndicesExist = "AzureSagaStorage.SagaStorageAssumeSecondaryIndicesExist";
        public const string SagaStorageAssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey = "AzureSagaStorage.SagaStorageAssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey";
        public const string SagaStorageCompatibilityMode = "AzureSagaStorage.EnableCompatibilityMode";
        public const string SubscriptionStorageTableName = "AzureSubscriptionStorage.TableName";
        public const string SubscriptionStorageCacheFor = "AzureSubscriptionStorage.CacheFor";
        public const string SubscriptionStorageCreateSchema = "AzureSubscriptionStorage.CreateSchema";
    }
}