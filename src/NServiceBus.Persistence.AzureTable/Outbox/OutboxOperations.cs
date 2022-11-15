namespace NServiceBus.Persistence.AzureTable
{
    using System.Collections.Generic;
    using Azure.Data.Tables;

    sealed class OutboxStore : Operation
    {
        public OutboxStore(TableEntityPartitionKey partitionKey, OutboxRecord outboxRow, TableClient tableClient) : base(partitionKey)
        {
            this.tableClient = tableClient;
            this.outboxRow = outboxRow;
        }

        public override TableClient Apply(List<TableTransactionAction> transactionalBatch)
        {
            transactionalBatch.Add(new TableTransactionAction(TableTransactionActionType.Add, outboxRow));
            return tableClient;
        }

        readonly OutboxRecord outboxRow;
        readonly TableClient tableClient;
    }

    sealed class OutboxDelete : Operation
    {
        public OutboxDelete(TableEntityPartitionKey partitionKey, OutboxRecord outboxRow, TableClient tableClient) : base(partitionKey)
        {
            this.tableClient = tableClient;
            this.outboxRow = outboxRow;
        }

        public override TableClient Apply(List<TableTransactionAction> transactionalBatch)
        {
            transactionalBatch.Add(new TableTransactionAction(TableTransactionActionType.UpdateReplace, outboxRow, outboxRow.ETag));
            return tableClient;
        }

        readonly OutboxRecord outboxRow;
        readonly TableClient tableClient;
    }
}