namespace NServiceBus.Persistence.AzureTable
{
    using System.Collections.Generic;
    using Azure.Data.Tables;

    class OutboxStore : Operation
    {
        public OutboxStore(TableEntityPartitionKey partitionKey, OutboxRecord outboxRow, TableClient cloudTable) : base(partitionKey)
        {
            this.cloudTable = cloudTable;
            this.outboxRow = outboxRow;
        }

        public override TableClient Apply(List<TableTransactionAction> transactionalBatch)
        {
            transactionalBatch.Add(new TableTransactionAction(TableTransactionActionType.Add, outboxRow));
            return cloudTable;
        }

        readonly OutboxRecord outboxRow;
        readonly TableClient cloudTable;
    }

    class OutboxDelete : Operation
    {
        public OutboxDelete(TableEntityPartitionKey partitionKey, OutboxRecord outboxRow, TableClient cloudTable) : base(partitionKey)
        {
            this.cloudTable = cloudTable;
            this.outboxRow = outboxRow;
        }

        public override TableClient Apply(List<TableTransactionAction> transactionalBatch)
        {
            transactionalBatch.Add(new TableTransactionAction(TableTransactionActionType.UpdateReplace, outboxRow));
            return cloudTable;
        }

        readonly OutboxRecord outboxRow;
        readonly TableClient cloudTable;
    }
}