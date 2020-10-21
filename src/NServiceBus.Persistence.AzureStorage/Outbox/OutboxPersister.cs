namespace NServiceBus.Persistence.AzureStorage
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Extensibility;
    using Outbox;


    class OutboxPersister  : IOutboxStorage
    {
        public OutboxPersister(TableHolderResolver tableHolderResolver)
        {
            this.tableHolderResolver = tableHolderResolver;
        }

        public Task<OutboxTransaction> BeginTransaction(ContextBag context)
        {
            var azureStorageOutboxTransaction = new AzureStorageOutboxTransaction(tableHolderResolver, context);

            if (context.TryGet<TableEntityPartitionKey>(out var partitionKey))
            {
                azureStorageOutboxTransaction.PartitionKey = partitionKey;
            }
            return Task.FromResult((OutboxTransaction)azureStorageOutboxTransaction);
        }

        public async Task<OutboxMessage> Get(string messageId, ContextBag context)
        {
            var setAsDispatchedHolder = new SetAsDispatchedHolder
            {
                TableHolder = tableHolderResolver.ResolveAndSetIfAvailable(context)
            };
            context.Set(setAsDispatchedHolder);

            if (!context.TryGet<TableEntityPartitionKey>(out var partitionKey))
            {
                // we return null here to enable outbox work at logical stage
                return null;
            }

            setAsDispatchedHolder.PartitionKey = partitionKey;

            var retrieveResult = await setAsDispatchedHolder.TableHolder.Table.ExecuteAsync(TableOperation.Retrieve<OutboxRecord>(partitionKey.PartitionKey, messageId))
                .ConfigureAwait(false);
            return retrieveResult.Result is OutboxRecord outboxRecord ? new OutboxMessage(outboxRecord.Id, outboxRecord.Operations) : null;
        }

        public Task Store(OutboxMessage message, OutboxTransaction transaction, ContextBag context)
        {
            var azureStorageOutboxTransaction = (AzureStorageOutboxTransaction)transaction;

            if (azureStorageOutboxTransaction == null || azureStorageOutboxTransaction.SuppressStoreAndCommit || azureStorageOutboxTransaction.PartitionKey == default)
            {
                return Task.CompletedTask;
            }

            var storeOperation = TableOperation.Insert(new OutboxRecord
            {
                Id = message.MessageId,
                Operations = message.TransportOperations,
                PartitionKey = azureStorageOutboxTransaction.PartitionKey.PartitionKey
            });

            azureStorageOutboxTransaction.StorageSession.Batch.Add(storeOperation);
            return Task.CompletedTask;
        }

        public async Task SetAsDispatched(string messageId, ContextBag context)
        {
            var setAsDispatchedHolder = context.Get<SetAsDispatchedHolder>();

            var partitionKey = setAsDispatchedHolder.PartitionKey;
            var tableHolder = setAsDispatchedHolder.TableHolder;

            var replaceOperation = TableOperation.Replace(new OutboxRecord
            {
                Id = messageId,
                Operations = Array.Empty<TransportOperation>(),
                PartitionKey = partitionKey.PartitionKey
            });

            // TODO inspect result
            await tableHolder.Table.ExecuteAsync(replaceOperation).ConfigureAwait(false);
        }

        TableHolderResolver tableHolderResolver;
    }
}