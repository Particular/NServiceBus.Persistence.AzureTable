namespace NServiceBus.Persistence.AzureTable
{
    using Microsoft.Azure.Cosmos.Table;

    class OutboxStore : Operation
    {
        public OutboxStore(TableEntityPartitionKey partitionKey, OutboxRecord outboxRow, CloudTable cloudTable) : base(partitionKey)
        {
            this.cloudTable = cloudTable;
            this.outboxRow = outboxRow;
        }

        public override CloudTable Apply(TableBatchOperation transactionalBatch)
        {
            transactionalBatch.Add(TableOperation.Insert(outboxRow));
            return cloudTable;
        }

        readonly OutboxRecord outboxRow;
        readonly CloudTable cloudTable;
    }

    class OutboxDelete : Operation
    {
        public OutboxDelete(TableEntityPartitionKey partitionKey, OutboxRecord outboxRow, CloudTable cloudTable) : base(partitionKey)
        {
            this.cloudTable = cloudTable;
            this.outboxRow = outboxRow;
        }

        public override CloudTable Apply(TableBatchOperation transactionalBatch)
        {
            transactionalBatch.Add(TableOperation.Replace(outboxRow));
            return cloudTable;
        }

        readonly OutboxRecord outboxRow;
        readonly CloudTable cloudTable;
    }
}