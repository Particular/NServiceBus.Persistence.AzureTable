namespace NServiceBus.Persistence.AzureTable
{
    using System.Collections.Generic;
    using Azure;
    using Azure.Data.Tables;

    abstract class Operation
    {
        protected Operation(TableEntityPartitionKey partitionKey) => PartitionKey = partitionKey;

        public TableEntityPartitionKey PartitionKey { get; }

        public abstract TableClient Apply(List<TableTransactionAction> transactionalBatch);

        public virtual void Success(Response result)
        {
        }

        public virtual void Conflict(Response result) => throw new TableBatchOperationException(result);

        public virtual bool Handle(RequestFailedException requestFailedException) => false;
    }

    sealed class ThrowOnConflictOperation : Operation
    {
        ThrowOnConflictOperation() : base(default)
        {
        }

        public static Operation Instance { get; } = new ThrowOnConflictOperation();

        public override TableClient Apply(List<TableTransactionAction> transactionalBatch) => null;
    }
}