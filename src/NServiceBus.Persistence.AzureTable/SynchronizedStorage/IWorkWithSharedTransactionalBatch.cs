namespace NServiceBus.Persistence.AzureTable
{
    using Extensibility;

    interface IWorkWithSharedTransactionalBatch : IAzureTableStorageSession
    {
        ContextBag CurrentContextBag { get; set; }
        void Add(Operation operation);
    }
}