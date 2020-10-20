namespace NServiceBus.Persistence.AzureStorage
{
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents;
    using Newtonsoft.Json;
    using NServiceBus.Extensibility;
    using NServiceBus.Outbox;


    class OutboxPersister  : IOutboxStorage
    {
        public OutboxPersister(TableHolderResolver tableHolderResolver)
        {
            this.tableHolderResolver = tableHolderResolver;
        }

        public Task<OutboxTransaction> BeginTransaction(ContextBag context)
        {
            var cosmosOutboxTransaction = new AzureStorageOutboxTransaction(tableHolderResolver, context);

            // if (context.TryGet<PartitionKey>(out var partitionKey))
            // {
            //     cosmosOutboxTransaction.PartitionKey = partitionKey;
            // }
            return Task.FromResult((OutboxTransaction)cosmosOutboxTransaction);
        }

        public async Task<OutboxMessage> Get(string messageId, ContextBag context)
        {
            var setAsDispatchedHolder = new SetAsDispatchedHolder
            {
                TableHolder = tableHolderResolver.ResolveAndSetIfAvailable(context)
            };
            context.Set(setAsDispatchedHolder);

            if (!context.TryGet<PartitionKey>(out var partitionKey))
            {
                // we return null here to enable outbox work at logical stage
                return null;
            }

            setAsDispatchedHolder.PartitionKey = partitionKey;

            OutboxRecord outboxRecord = null;
            // var outboxRecord = await setAsDispatchedHolder.TableHolder.Table.ReadOutboxRecord(messageId, partitionKey, serializer, context)
            //     .ConfigureAwait(false);
            return outboxRecord != null ? new OutboxMessage(outboxRecord.Id, /*outboxRecord.TransportOperations*/ null) : null;
        }

        public Task Store(OutboxMessage message, OutboxTransaction transaction, ContextBag context)
        {
            var cosmosTransaction = (AzureStorageOutboxTransaction)transaction;

            if (cosmosTransaction == null || cosmosTransaction.SuppressStoreAndCommit || cosmosTransaction.PartitionKey == null)
            {
                return Task.CompletedTask;
            }

            // cosmosTransaction.StorageSession.AddOperation(new OutboxStore(new OutboxRecord
            //     {
            //         Id = message.MessageId,
            //         TransportOperations = message.TransportOperations
            //     },
            //     cosmosTransaction.PartitionKey.Value,
            //     serializer,
            //     context));
            return Task.CompletedTask;
        }

        public Task SetAsDispatched(string messageId, ContextBag context)
        {
            var setAsDispatchedHolder = context.Get<SetAsDispatchedHolder>();

            var partitionKey = setAsDispatchedHolder.PartitionKey;
            var containerHolder = setAsDispatchedHolder.TableHolder;

            // var operation = new OutboxDelete(new OutboxRecord
            // {
            //     Id = messageId,
            //     Dispatched = true
            // }, partitionKey, serializer, ttlInSeconds, context);
            //
            // var transactionalBatch = containerHolder.Container.CreateTransactionalBatch(partitionKey);
            //
            // await transactionalBatch.ExecuteOperationAsync(operation, containerHolder.PartitionKeyPath).ConfigureAwait(false);
            return Task.CompletedTask;
        }

        TableHolderResolver tableHolderResolver;
    }
}