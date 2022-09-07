namespace NServiceBus.Persistence.AzureTable
{
    using System.Threading.Tasks;
    using Extensibility;

    class StorageSessionFactory : ISynchronizedStorage
    {
        public StorageSessionFactory(TableHolderResolver tableHolderResolver)
        {
            this.tableHolderResolver = tableHolderResolver;
        }

        public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
        {
            var storageSession = new StorageSession(tableHolderResolver, contextBag, true);
            return Task.FromResult<CompletableSynchronizedStorageSession>(storageSession);
        }
        
        readonly TableHolderResolver tableHolderResolver;
    }
}