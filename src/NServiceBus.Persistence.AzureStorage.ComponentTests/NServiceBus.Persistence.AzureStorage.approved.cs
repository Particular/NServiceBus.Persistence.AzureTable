[assembly: System.Runtime.CompilerServices.InternalsVisibleToAttribute(@"NServiceBus.Persistence.AzureStorage.ComponentTests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100dde965e6172e019ac82c2639ffe494dd2e7dd16347c34762a05732b492e110f2e4e2e1b5ef2d85c848ccfb671ee20a47c8d1376276708dc30a90ff1121b647ba3b7259a6bc383b2034938ef0e275b58b920375ac605076178123693c6c4f1331661a62eba28c249386855637780e3ff5f23a6d854700eaa6803ef48907513b92")]
[assembly: System.Runtime.Versioning.TargetFrameworkAttribute(".NETFramework,Version=v4.5.2", FrameworkDisplayName=".NET Framework 4.5.2")]

namespace NServiceBus
{
    
    public class AzureStoragePersistence : NServiceBus.Persistence.PersistenceDefinition { }
    public class AzureStorageSagaPersistence : NServiceBus.Features.Feature
    {
        protected override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
    public class AzureStorageSubscriptionPersistence : NServiceBus.Features.Feature
    {
        protected override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
    public class AzureStorageTimeoutPersistence : NServiceBus.Features.Feature
    {
        protected override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
    public class static ConfigureAzureSagaStorage
    {
        public static NServiceBus.PersistenceExtensions<NServiceBus.AzureStoragePersistence, NServiceBus.Persistence.StorageType.Sagas> AssumeSecondaryIndicesExist(this NServiceBus.PersistenceExtensions<NServiceBus.AzureStoragePersistence, NServiceBus.Persistence.StorageType.Sagas> config) { }
        public static NServiceBus.PersistenceExtensions<NServiceBus.AzureStoragePersistence, NServiceBus.Persistence.StorageType.Sagas> ConnectionString(this NServiceBus.PersistenceExtensions<NServiceBus.AzureStoragePersistence, NServiceBus.Persistence.StorageType.Sagas> config, string connectionString) { }
        public static NServiceBus.PersistenceExtensions<NServiceBus.AzureStoragePersistence, NServiceBus.Persistence.StorageType.Sagas> CreateSchema(this NServiceBus.PersistenceExtensions<NServiceBus.AzureStoragePersistence, NServiceBus.Persistence.StorageType.Sagas> config, bool createSchema) { }
    }
    public class static ConfigureAzureStorage
    {
        public static NServiceBus.PersistenceExtensions<NServiceBus.AzureStoragePersistence> ConnectionString(this NServiceBus.PersistenceExtensions<NServiceBus.AzureStoragePersistence> config, string connectionString) { }
    }
    public class static ConfigureAzureSubscriptionStorage
    {
        public static NServiceBus.PersistenceExtensions<NServiceBus.AzureStoragePersistence, NServiceBus.Persistence.StorageType.Subscriptions> CacheFor(this NServiceBus.PersistenceExtensions<NServiceBus.AzureStoragePersistence, NServiceBus.Persistence.StorageType.Subscriptions> config, System.TimeSpan timeSpan) { }
        public static NServiceBus.PersistenceExtensions<NServiceBus.AzureStoragePersistence, NServiceBus.Persistence.StorageType.Subscriptions> ConnectionString(this NServiceBus.PersistenceExtensions<NServiceBus.AzureStoragePersistence, NServiceBus.Persistence.StorageType.Subscriptions> config, string connectionString) { }
        public static NServiceBus.PersistenceExtensions<NServiceBus.AzureStoragePersistence, NServiceBus.Persistence.StorageType.Subscriptions> CreateSchema(this NServiceBus.PersistenceExtensions<NServiceBus.AzureStoragePersistence, NServiceBus.Persistence.StorageType.Subscriptions> config, bool createSchema) { }
        public static NServiceBus.PersistenceExtensions<NServiceBus.AzureStoragePersistence, NServiceBus.Persistence.StorageType.Subscriptions> TableName(this NServiceBus.PersistenceExtensions<NServiceBus.AzureStoragePersistence, NServiceBus.Persistence.StorageType.Subscriptions> config, string tableName) { }
    }
    public class static ConfigureAzureTimeoutStorage
    {
        public static NServiceBus.PersistenceExtensions<NServiceBus.AzureStoragePersistence, NServiceBus.Persistence.StorageType.Timeouts> CatchUpInterval(this NServiceBus.PersistenceExtensions<NServiceBus.AzureStoragePersistence, NServiceBus.Persistence.StorageType.Timeouts> config, int catchUpInterval) { }
        public static NServiceBus.PersistenceExtensions<NServiceBus.AzureStoragePersistence, NServiceBus.Persistence.StorageType.Timeouts> ConnectionString(this NServiceBus.PersistenceExtensions<NServiceBus.AzureStoragePersistence, NServiceBus.Persistence.StorageType.Timeouts> config, string connectionString) { }
        public static NServiceBus.PersistenceExtensions<NServiceBus.AzureStoragePersistence, NServiceBus.Persistence.StorageType.Timeouts> CreateSchema(this NServiceBus.PersistenceExtensions<NServiceBus.AzureStoragePersistence, NServiceBus.Persistence.StorageType.Timeouts> config, bool createSchema) { }
        public static NServiceBus.PersistenceExtensions<NServiceBus.AzureStoragePersistence, NServiceBus.Persistence.StorageType.Timeouts> PartitionKeyScope(this NServiceBus.PersistenceExtensions<NServiceBus.AzureStoragePersistence, NServiceBus.Persistence.StorageType.Timeouts> config, string partitionKeyScope) { }
        public static NServiceBus.PersistenceExtensions<NServiceBus.AzureStoragePersistence, NServiceBus.Persistence.StorageType.Timeouts> TimeoutDataTableName(this NServiceBus.PersistenceExtensions<NServiceBus.AzureStoragePersistence, NServiceBus.Persistence.StorageType.Timeouts> config, string tableName) { }
        public static NServiceBus.PersistenceExtensions<NServiceBus.AzureStoragePersistence, NServiceBus.Persistence.StorageType.Timeouts> TimeoutManagerDataTableName(this NServiceBus.PersistenceExtensions<NServiceBus.AzureStoragePersistence, NServiceBus.Persistence.StorageType.Timeouts> config, string tableName) { }
        public static NServiceBus.PersistenceExtensions<NServiceBus.AzureStoragePersistence, NServiceBus.Persistence.StorageType.Timeouts> TimeoutStateContainerName(this NServiceBus.PersistenceExtensions<NServiceBus.AzureStoragePersistence, NServiceBus.Persistence.StorageType.Timeouts> config, string blobName) { }
    }
}
namespace NServiceBus.Persistence.AzureStorage
{
    
    public class DuplicatedSagaFoundException : System.Exception
    {
        public DuplicatedSagaFoundException(System.Type sagaType, string propertyName, params System.Guid[] identifiers) { }
        public System.Guid[] Identifiers { get; }
        public string PropertyName { get; }
        public System.Type SagaType { get; }
    }
    public class RetryNeededException : System.Exception
    {
        public RetryNeededException() { }
        public RetryNeededException(System.Exception innerException) { }
    }
}