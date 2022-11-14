namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using Azure;
    using Azure.Data.Tables;
    using Logging;

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

    sealed class SagaRemoveSecondaryIndex : Operation
    {
        public SagaRemoveSecondaryIndex(TableEntityPartitionKey partitionKey, Guid sagaId, SecondaryIndex secondaryIndices, PartitionRowKeyTuple partitionRowKeyTuple, TableClient table) : base(partitionKey)
        {
            this.sagaId = sagaId;
            this.partitionRowKeyTuple = partitionRowKeyTuple;
            this.table = table;
            this.secondaryIndices = secondaryIndices;
        }

        public override TableClient Apply(List<TableTransactionAction> transactionalBatch)
        {
            var e = new TableEntity
            {
                ETag = ETag.All
            };

            partitionRowKeyTuple.Apply(e);
            secondaryIndices.InvalidateCache(partitionRowKeyTuple);
            transactionalBatch.Add(new TableTransactionAction(TableTransactionActionType.Delete, e));
            return table;
        }

        public override bool Handle(RequestFailedException requestFailedException)
        {
            if (requestFailedException.Status == (int)HttpStatusCode.NotFound)
            {
                Logger.Warn($"Removal of the secondary index entry for the following saga failed: '{sagaId}'");
            }
            return true;
        }

        readonly PartitionRowKeyTuple partitionRowKeyTuple;
        readonly Guid sagaId;
        readonly SecondaryIndex secondaryIndices;
        readonly TableClient table;
        static readonly ILog Logger = LogManager.GetLogger<AzureSagaPersister>();
    }
}