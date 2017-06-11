[assembly: System.Runtime.CompilerServices.InternalsVisibleToAttribute(@"NServiceBus.Persistence.AzureStorage.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100dde965e6172e019ac82c2639ffe494dd2e7dd16347c34762a05732b492e110f2e4e2e1b5ef2d85c848ccfb671ee20a47c8d1376276708dc30a90ff1121b647ba3b7259a6bc383b2034938ef0e275b58b920375ac605076178123693c6c4f1331661a62eba28c249386855637780e3ff5f23a6d854700eaa6803ef48907513b92")]
[assembly: System.Runtime.InteropServices.ComVisibleAttribute(false)]
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
    public class DetectObsoleteConfigurationSettings : NServiceBus.Features.Feature
    {
        public DetectObsoleteConfigurationSettings() { }
        protected override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
}
namespace NServiceBus.Config
{
    
    public class AzureSagaPersisterConfig : System.Configuration.ConfigurationSection
    {
        public AzureSagaPersisterConfig() { }
        [System.ObsoleteAttribute("Switch to the code API by Using `PersistenceExtensions<AzureStoragePersistence>.C" +
            "onnectionString` instead. Will be treated as an error from version 1.0.0. Will b" +
            "e removed in version 2.0.0.", false)]
        public string ConnectionString { get; set; }
        [System.ObsoleteAttribute("Switch to the code API by Using `PersistenceExtensions<AzureStoragePersistence>.C" +
            "reateSchema` instead. Will be treated as an error from version 1.0.0. Will be re" +
            "moved in version 2.0.0.", false)]
        public bool CreateSchema { get; set; }
    }
    public class AzureSubscriptionStorageConfig : System.Configuration.ConfigurationSection
    {
        public AzureSubscriptionStorageConfig() { }
        [System.ObsoleteAttribute("Switch to the code API by Using `PersistenceExtentions<AzureStoragePersistence>.C" +
            "onnectionString` instead. Will be treated as an error from version 1.0.0. Will b" +
            "e removed in version 2.0.0.", false)]
        public string ConnectionString { get; set; }
        [System.ObsoleteAttribute("Switch to the code API by Using `PersistenceExtentions<AzureStoragePersistence>.C" +
            "reateSchema` instead. Will be treated as an error from version 1.0.0. Will be re" +
            "moved in version 2.0.0.", false)]
        public bool CreateSchema { get; set; }
        [System.ObsoleteAttribute("Switch to the code API by Using `PersistenceExtentions<AzureStoragePersistence>.T" +
            "ableName` instead. Will be treated as an error from version 1.0.0. Will be remov" +
            "ed in version 2.0.0.", false)]
        public string TableName { get; set; }
    }
    public class AzureTimeoutPersisterConfig : System.Configuration.ConfigurationSection
    {
        public AzureTimeoutPersisterConfig() { }
        [System.ObsoleteAttribute("Switch to the code API by Using `PersistenceExtensions<AzureStoragePersistence>.C" +
            "atchUpInterval` instead. Will be treated as an error from version 1.0.0. Will be" +
            " removed in version 2.0.0.", false)]
        public int CatchUpInterval { get; set; }
        [System.ObsoleteAttribute("Switch to the code API by Using `PersistenceExtensions<AzureStoragePersistence>.C" +
            "onnectionString` instead. Will be treated as an error from version 1.0.0. Will b" +
            "e removed in version 2.0.0.", false)]
        public string ConnectionString { get; set; }
        [System.ObsoleteAttribute("Switch to the code API by Using `PersistenceExtensions<AzureStoragePersistence>.C" +
            "reateSchema` instead. Will be treated as an error from version 1.0.0. Will be re" +
            "moved in version 2.0.0.", false)]
        public bool CreateSchema { get; set; }
        [System.ObsoleteAttribute("Switch to the code API by Using `PersistenceExtensions<AzureStoragePersistence>.C" +
            "onnectionString` instead. Will be treated as an error from version 1.0.0. Will b" +
            "e removed in version 2.0.0.", false)]
        public string PartitionKeyScope { get; set; }
        [System.ObsoleteAttribute("Switch to the code API by Using `PersistenceExtensions<AzureStoragePersistence>.T" +
            "imeoutDataTableName` instead. Will be treated as an error from version 1.0.0. Wi" +
            "ll be removed in version 2.0.0.", false)]
        public string TimeoutDataTableName { get; set; }
        [System.ObsoleteAttribute("Switch to the code API by Using `PersistenceExtensions<AzureStoragePersistence>.T" +
            "imeoutManagerDataTableName` instead. Will be treated as an error from version 1." +
            "0.0. Will be removed in version 2.0.0.", false)]
        public string TimeoutManagerDataTableName { get; set; }
        [System.ObsoleteAttribute("Switch to the code API by Using `PersistenceExtensions<AzureStoragePersistence>.T" +
            "imeoutStateContainerName` instead. Will be treated as an error from version 1.0." +
            "0. Will be removed in version 2.0.0.", false)]
        public string TimeoutStateContainerName { get; set; }
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