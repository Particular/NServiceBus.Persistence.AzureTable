﻿namespace NServiceBus.Testing
{
    using System.Collections.Generic;
    using Azure.Data.Tables;
    using Extensibility;
    using Persistence;
    using Persistence.AzureTable;

    /// <summary>
    /// A fake implementation for <see cref="SynchronizedStorageSession"/> for testing purposes.
    /// </summary>
    public partial class TestableAzureTableStorageSession : ISynchronizedStorageSession, IWorkWithSharedTransactionalBatch
    {
        /// <summary>
        /// Initializes a new TestableAzureTableStorageSession with a partition key.
        /// </summary>
        public TestableAzureTableStorageSession(TableEntityPartitionKey partitionKey)
        {
            var contextBag = new ContextBag();
            contextBag.Set(partitionKey);
            ((IWorkWithSharedTransactionalBatch)this).CurrentContextBag = contextBag;
            PartitionKey = partitionKey.PartitionKey;
        }

        ContextBag IWorkWithSharedTransactionalBatch.CurrentContextBag { get; set; }

        void IWorkWithSharedTransactionalBatch.Add(Operation operation)
        {
            if (BatchOperations == null)
            {
                return;
            }

            operation.Apply(BatchOperations);
        }

        /// <summary>
        /// The table client to be used.
        /// </summary>
        public TableClient TableClient { get; set; }

        /// <summary>
        /// The batch to be used.
        /// </summary>
        public List<TableTransactionAction> BatchOperations { get; set; }

        /// <summary>
        /// The partition key to be used.
        /// </summary>
        public string PartitionKey { get; }
    }
}