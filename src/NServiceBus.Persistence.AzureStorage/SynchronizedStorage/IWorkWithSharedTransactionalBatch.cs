namespace NServiceBus.Persistence.AzureStorage
{
    using Extensibility;

    interface IWorkWithSharedTransactionalBatch : IAzureStorageStorageSession
    {
        ContextBag CurrentContextBag { get; set; }
    }
}