namespace NServiceBus.Persistence.AzureStorage
{
    using Microsoft.Azure.Cosmos.Table;

    interface IWorkWithSharedTransactionalBatch
    {
        CloudTable Table { get; }

        TableBatchOperation Batch { get; }
    }
}