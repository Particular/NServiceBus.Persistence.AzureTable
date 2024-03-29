﻿namespace NServiceBus.Persistence.AzureTable
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Data.Tables;
    using Extensibility;
    using Outbox;
    using Transport;

    class AzureStorageSynchronizedStorageSession : ICompletableSynchronizedStorageSession, IWorkWithSharedTransactionalBatch
    {
        public AzureStorageSynchronizedStorageSession(TableClientHolderResolver tableClientHolderResolver)
            => this.tableClientHolderResolver = tableClientHolderResolver;

        public void Dispose()
        {
            if (!disposed && ownsTransaction)
            {
                session.Dispose();
                disposed = true;
            }
        }

        public ValueTask<bool> TryOpen(IOutboxTransaction transaction, ContextBag context, CancellationToken cancellationToken = default)
        {
            if (transaction is AzureStorageOutboxTransaction azureStorageOutboxTransaction)
            {
                session = azureStorageOutboxTransaction.StorageSession;
                session.CurrentContextBag = context;
                ownsTransaction = false;
                return new ValueTask<bool>(true);
            }
            return new ValueTask<bool>(false);
        }

        public ValueTask<bool> TryOpen(TransportTransaction transportTransaction, ContextBag context,
                                       CancellationToken cancellationToken = default) =>
            new ValueTask<bool>(false);

        public Task Open(ContextBag contextBag, CancellationToken cancellationToken = default)
        {
            ownsTransaction = true;
            session = new StorageSession(tableClientHolderResolver, contextBag);
            return Task.CompletedTask;
        }

        public Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            if (ownsTransaction)
            {
                return session.Commit(cancellationToken);
            }
            return Task.CompletedTask;
        }

        readonly TableClientHolderResolver tableClientHolderResolver;
        bool disposed;
        StorageSession session;
        bool ownsTransaction;
        public TableClient Table => session.Table;
        public List<TableTransactionAction> Batch => session.Batch;
        public string PartitionKey => session.PartitionKey;
        public ContextBag CurrentContextBag
        {
            get => session.CurrentContextBag;
            set => session.CurrentContextBag = value;
        }

        public void Add(Operation operation) => session.Add(operation);
    }
}