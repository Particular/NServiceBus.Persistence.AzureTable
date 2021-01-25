namespace NServiceBus.Persistence.AzureTable
{
    using Microsoft.Azure.Cosmos.Table;

    abstract class Operation
    {
        protected Operation(TableEntityPartitionKey partitionKey)
        {
            PartitionKey = partitionKey;
        }

        public TableEntityPartitionKey PartitionKey { get; }

        public abstract CloudTable Apply(TableBatchOperation transactionalBatch);

        public virtual void Success(TableResult result)
        {
        }

        public virtual void Conflict(TableResult result)
        {
            throw new TableBatchOperationException(result);
        }

        public virtual bool Handle(StorageException storageException)
        {
            return false;
        }
    }

    class ThrowOnConflictOperation : Operation
    {
        ThrowOnConflictOperation() : base(default)
        {
        }

        public static Operation Instance { get; } = new ThrowOnConflictOperation();

        public override CloudTable Apply(TableBatchOperation transactionalBatch)
        {
            return null;
        }
    }
}