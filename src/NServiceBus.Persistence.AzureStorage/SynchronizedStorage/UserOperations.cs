namespace NServiceBus.Persistence.AzureStorage
{
    using Microsoft.Azure.Cosmos.Table;

    class UserOperation : Operation
    {
        public UserOperation(TableEntityPartitionKey partitionKey, CloudTable table, TableOperation tableOperation) : base(partitionKey)
        {
            this.tableOperation = tableOperation;
            this.table = table;
        }

        public override CloudTable Apply(TableBatchOperation transactionalBatch)
        {
            transactionalBatch.Add(tableOperation);
            return table;
        }

        private CloudTable table;
        private readonly TableOperation tableOperation;
    }
}