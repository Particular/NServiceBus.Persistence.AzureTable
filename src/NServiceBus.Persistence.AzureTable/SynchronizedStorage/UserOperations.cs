namespace NServiceBus.Persistence.AzureTable
{
    using System.Collections.Generic;
    using Azure.Data.Tables;

    sealed class UserOperation : Operation
    {
        public UserOperation(TableEntityPartitionKey partitionKey, TableClient table, TableTransactionAction tableOperation) : base(partitionKey)
        {
            this.tableOperation = tableOperation;
            this.table = table;
        }

        public override TableClient Apply(List<TableTransactionAction> transactionalBatch)
        {
            transactionalBatch.Add(tableOperation);
            return table;
        }

        readonly TableClient table;
        readonly TableTransactionAction tableOperation;
    }
}