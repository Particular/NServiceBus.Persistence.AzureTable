namespace NServiceBus
{
    using System.Threading.Tasks;
    using Features;
    using Extensibility;
    using Persistence;

    public class AzureStoragePersistence : PersistenceDefinition
    {
        internal AzureStoragePersistence()
        {
            Supports<StorageType.Timeouts>(s => s.EnableFeatureByDefault<AzureStorageTimeoutPersistence>());
            Supports<StorageType.Sagas>(s => s.EnableFeatureByDefault<AzureStorageSagaPersistence>());
            Supports<StorageType.Subscriptions>(s => s.EnableFeatureByDefault<AzureStorageSubscriptionPersistence>());
        }
    }

    // Use this to disable synchronization storage by providing an empty implementation
    // so that when the container tries to resolve an instance if ISynchronizedStorage it
    // doesn't throw a ComponentNotRegisteredException exception
    // This is hacky and we need a proper way around this.
    public class FakeSynchronizationStorage : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<ISynchronizedStorage>(() => new AzureSynchronizedStorage(), DependencyLifecycle.SingleInstance);
        }
    }

    // Use this to disable synchronization storage by providing an empty implementation
    // so that when the container tries to resolve an instance if ISynchronizedStorage it
    // doesn't throw a ComponentNotRegisteredException exception
    // This is hacky and we need a proper way around this.
    public class AzureSynchronizedStorage : ISynchronizedStorage, CompletableSynchronizedStorageSession
    {
        public Task CompleteAsync()
        {
            return Task.FromResult(0);
        }

        public void Dispose()
        {
        }

        public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
        {
            return Task.FromResult((CompletableSynchronizedStorageSession)this);
        }
    }
}