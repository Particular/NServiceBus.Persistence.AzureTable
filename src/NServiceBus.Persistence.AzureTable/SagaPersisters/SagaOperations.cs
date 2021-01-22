namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.Net;
    using Microsoft.Azure.Cosmos.Table;
    using Logging;

    class SagaSave : Operation
    {
        readonly DictionaryTableEntity sagaRow;

        public SagaSave(TableEntityPartitionKey partitionKey, DictionaryTableEntity sagaRow) : base(partitionKey)
        {
            this.sagaRow = sagaRow;
        }

        public override CloudTable Apply(TableBatchOperation transactionalBatch)
        {
            transactionalBatch.Add(TableOperation.Insert(sagaRow));
            return sagaRow.Table;
        }
    }

    class SagaUpdate : Operation
    {
        readonly DictionaryTableEntity sagaRow;

        public SagaUpdate(TableEntityPartitionKey partitionKey, DictionaryTableEntity sagaRow) : base(partitionKey)
        {
            this.sagaRow = sagaRow;
        }

        public override CloudTable Apply(TableBatchOperation transactionalBatch)
        {
            transactionalBatch.Add(TableOperation.Replace(sagaRow));
            return sagaRow.Table;
        }
    }

    class SagaDelete : Operation
    {
        readonly DictionaryTableEntity sagaRow;

        public SagaDelete(TableEntityPartitionKey partitionKey, DictionaryTableEntity sagaRow) : base(partitionKey)
        {
            this.sagaRow = sagaRow;
        }

        public override CloudTable Apply(TableBatchOperation transactionalBatch)
        {
            transactionalBatch.Add(TableOperation.Delete(sagaRow));
            return sagaRow.Table;
        }
    }

    class SagaRemoveSecondaryIndex : Operation
    {
        readonly SecondaryIndex secondaryIndices;
        readonly CloudTable table;

        public SagaRemoveSecondaryIndex(TableEntityPartitionKey partitionKey, Guid sagaId, SecondaryIndex secondaryIndices, PartitionRowKeyTuple partitionRowKeyTuple, CloudTable table) : base(partitionKey)
        {
            this.sagaId = sagaId;
            this.partitionRowKeyTuple = partitionRowKeyTuple;
            this.table = table;
            this.secondaryIndices = secondaryIndices;
        }

        public override CloudTable Apply(TableBatchOperation transactionalBatch)
        {
            var e = new TableEntity
            {
                ETag = "*"
            };

            partitionRowKeyTuple.Apply(e);
            secondaryIndices.InvalidateCache(partitionRowKeyTuple);
            transactionalBatch.Add(TableOperation.Delete(e));
            return table;
        }

        public override bool Handle(StorageException storageException)
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