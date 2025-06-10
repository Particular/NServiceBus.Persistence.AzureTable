namespace NServiceBus.Persistence.AzureTable
{
    using System.Collections.Generic;
    using Azure.Data.Tables;

    sealed class SagaSave : Operation
    {
        public SagaSave(TableEntityPartitionKey partitionKey, TableEntity sagaRow, TableClient tableClient) : base(partitionKey)
        {
            this.sagaRow = sagaRow;
            this.tableClient = tableClient;
        }

        public override TableClient Apply(List<TableTransactionAction> transactionalBatch)
        {
            transactionalBatch.Add(new TableTransactionAction(TableTransactionActionType.Add, sagaRow));
            return tableClient;
        }

        readonly TableEntity sagaRow;
        readonly TableClient tableClient;
    }

    sealed class SagaUpdate : Operation
    {
        public SagaUpdate(TableEntityPartitionKey partitionKey, TableEntity sagaRow, TableClient tableClient) : base(partitionKey)
        {
            this.sagaRow = sagaRow;
            this.tableClient = tableClient;
        }

        public override TableClient Apply(List<TableTransactionAction> transactionalBatch)
        {
            transactionalBatch.Add(new TableTransactionAction(TableTransactionActionType.UpdateReplace, sagaRow, sagaRow.ETag));
            return tableClient;
        }

        readonly TableEntity sagaRow;
        readonly TableClient tableClient;
    }

    sealed class SagaDelete : Operation
    {
        public SagaDelete(TableEntityPartitionKey partitionKey, TableEntity sagaRow, TableClient tableClient) : base(partitionKey)
        {
            this.sagaRow = sagaRow;
            this.tableClient = tableClient;
        }

        public override TableClient Apply(List<TableTransactionAction> transactionalBatch)
        {
            transactionalBatch.Add(new TableTransactionAction(TableTransactionActionType.Delete, sagaRow, sagaRow.ETag));
            return tableClient;
        }

        readonly TableEntity sagaRow;
        readonly TableClient tableClient;
    }
}