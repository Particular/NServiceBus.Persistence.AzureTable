namespace NServiceBus.Persistence.AzureTable
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Data.Tables;
    using Extensibility;
    using NServiceBus.Transport;
    using Outbox;

    class OutboxPersister : IOutboxStorage
    {
        public OutboxPersister(TableClientHolderResolver tableClientHolderResolver)
            => this.tableClientHolderResolver = tableClientHolderResolver;

        public Task<IOutboxTransaction> BeginTransaction(ContextBag context, CancellationToken cancellationToken = default)
        {
            var azureStorageOutboxTransaction = new AzureStorageOutboxTransaction(tableClientHolderResolver, context);

            if (context.TryGet<TableEntityPartitionKey>(out var partitionKey))
            {
                azureStorageOutboxTransaction.PartitionKey = partitionKey;
            }
            return Task.FromResult((IOutboxTransaction)azureStorageOutboxTransaction);
        }

        public async Task<OutboxMessage> Get(string messageId, ContextBag context, CancellationToken cancellationToken = default)
        {
            var setAsDispatchedHolder = new SetAsDispatchedHolder
            {
                TableClientHolder = tableClientHolderResolver.ResolveAndSetIfAvailable(context)
            };
            context.Set(setAsDispatchedHolder);

            if (!context.TryGet<TableEntityPartitionKey>(out var partitionKey))
            {
                // because of the transactional session we cannot assume the incoming message is always present
                if (!context.TryGet<IncomingMessage>(out var incomingMessage) ||
                    !incomingMessage.Headers.ContainsKey(Headers.ControlMessageHeader))
                {
                    // we return null here to enable outbox work at logical stage
                    return null;
                }

                partitionKey = new TableEntityPartitionKey(messageId);
                context.Set(partitionKey);
            }

            setAsDispatchedHolder.ThrowIfTableClientIsNotSet();

            var outboxRecord = await setAsDispatchedHolder.TableClientHolder.TableClient
                .ReadOutboxRecord(messageId, partitionKey, context, cancellationToken)
                .ConfigureAwait(false);

            setAsDispatchedHolder.Record = outboxRecord;
            setAsDispatchedHolder.PartitionKey = partitionKey;

            return outboxRecord != null ? new OutboxMessage(outboxRecord.Id, outboxRecord.Operations) : null;
        }

        public Task Store(OutboxMessage message, IOutboxTransaction transaction, ContextBag context, CancellationToken cancellationToken = default)
        {
            var azureStorageOutboxTransaction = (AzureStorageOutboxTransaction)transaction;

            if (azureStorageOutboxTransaction == null || azureStorageOutboxTransaction.SuppressStoreAndCommit || azureStorageOutboxTransaction.PartitionKey == null)
            {
                return Task.CompletedTask;
            }

            var setAsDispatchedHolder = context.Get<SetAsDispatchedHolder>();
            setAsDispatchedHolder.ThrowIfTableClientIsNotSet();

            var outboxRecord = new OutboxRecord
            {
                Id = message.MessageId,
                Operations = message.TransportOperations,
                PartitionKey = setAsDispatchedHolder.PartitionKey.PartitionKey
            };

            setAsDispatchedHolder.Record = outboxRecord;

            azureStorageOutboxTransaction.StorageSession.Add(new OutboxStore(setAsDispatchedHolder.PartitionKey, outboxRecord, setAsDispatchedHolder.TableClientHolder.TableClient));

            return Task.CompletedTask;
        }

        public Task SetAsDispatched(string messageId, ContextBag context, CancellationToken cancellationToken = default)
        {
            var setAsDispatchedHolder = context.Get<SetAsDispatchedHolder>();

            var tableHolder = setAsDispatchedHolder.TableClientHolder;
            var record = setAsDispatchedHolder.Record;

            record.SetAsDispatched();

            var operation = new OutboxDelete(setAsDispatchedHolder.PartitionKey, record, tableHolder.TableClient);
            // Capacity is set to one with the knowledge that outbox delete only adds one action
            var transactionalBatch = new List<TableTransactionAction>(1);
            return transactionalBatch.ExecuteOperationAsync(operation, cancellationToken);
        }

        readonly TableClientHolderResolver tableClientHolderResolver;
    }
}