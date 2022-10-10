namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using Azure;
    using Azure.Data.Tables;
    using Logging;

    class SagaSave : Operation
    {
        readonly TableEntity sagaRow;

        public SagaSave(TableEntityPartitionKey partitionKey, TableEntity sagaRow) : base(partitionKey)
        {
            this.sagaRow = sagaRow;
        }

        public override TableClient Apply(List<TableTransactionAction> transactionalBatch)
        {
            transactionalBatch.Add(new TableTransactionAction(TableTransactionActionType.Add, sagaRow));
            return sagaRow.Table;
        }
    }

    class SagaUpdate : Operation
    {
        readonly TableEntity sagaRow;

        public SagaUpdate(TableEntityPartitionKey partitionKey, TableEntity sagaRow) : base(partitionKey)
        {
            this.sagaRow = sagaRow;
        }

        public override TableClient Apply(List<TableTransactionAction> transactionalBatch)
        {
            transactionalBatch.Add(new TableTransactionAction(TableTransactionActionType.UpdateReplace, sagaRow));
            return sagaRow.Table;
        }
    }

    class SagaDelete : Operation
    {
        readonly TableEntity sagaRow;

        public SagaDelete(TableEntityPartitionKey partitionKey, TableEntity sagaRow) : base(partitionKey)
        {
            this.sagaRow = sagaRow;
        }

        public override TableClient Apply(List<TableTransactionAction> transactionalBatch)
        {
            transactionalBatch.Add(new TableTransactionAction(TableTransactionActionType.Delete, sagaRow));
            return sagaRow.Table;
        }
    }

    class SagaRemoveSecondaryIndex : Operation
    {
        readonly SecondaryIndex secondaryIndices;
        readonly TableClient table;

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

        public override bool Handle(RequestFailedException storageException)
        {
            // Horrible logic to check if item has already been deleted or not
            var webException = storageException.InnerException as WebException;
            if (webException?.Response != null)
            {
                var response = (HttpWebResponse)webException.Response;
                if ((int)response.StatusCode != 404)
                {
                    // Was not a previously deleted exception
                    Logger.Warn($"Removal of the secondary index entry for the following saga failed: '{sagaId}'");
                }
            }
            return true;
        }

        readonly ILog Logger = LogManager.GetLogger<AzureSagaPersister>();
        PartitionRowKeyTuple partitionRowKeyTuple;
        readonly Guid sagaId;
    }
}