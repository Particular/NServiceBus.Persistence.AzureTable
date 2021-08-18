﻿namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading.Tasks;
    using Outbox;
    using Pipeline;
    using Routing;
    using Transport;
    using TransportOperation = Transport.TransportOperation;

    /// <summary>
    /// Mimics the outbox behavior as part of the logical phase. This type is public so that it isn't renamed and it can be used to register logical behaviors before this behavior
    /// </summary>
    public sealed class LogicalOutboxBehavior : IBehavior<IIncomingLogicalMessageContext, IIncomingLogicalMessageContext>
    {
        static LogicalOutboxBehavior()
        {
            var field = typeof(PendingTransportOperations).GetField("operations", BindingFlags.NonPublic | BindingFlags.Instance);
            var targetExp = Expression.Parameter(typeof(PendingTransportOperations), "target");
            var fieldExp = Expression.Field(targetExp, field);
            var assignExp = Expression.Assign(fieldExp, Expression.Constant(new ConcurrentStack<TransportOperation>()));

            setter = Expression.Lambda<Action<PendingTransportOperations>>(assignExp, targetExp).Compile();
        }

        internal LogicalOutboxBehavior(TableHolderResolver tableHolderResolver)
        {
            this.tableHolderResolver = tableHolderResolver;
        }

        /// <inheritdoc />
        public async Task Invoke(IIncomingLogicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> next)
        {
            if (!context.Extensions.TryGet<IOutboxTransaction>(out var transaction))
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            if (!(transaction is AzureStorageOutboxTransaction IOutboxTransaction))
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            // Normal outbox operating at the physical stage
            if (IOutboxTransaction.PartitionKey.HasValue)
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            // Outbox operating at the logical stage
            if (!context.Extensions.TryGet<TableEntityPartitionKey>(out var partitionKey))
            {
                throw new Exception("For the outbox to work the following information must be provided at latest up to the incoming physical or logical message stage. A partition key via `context.Extensions.Set<PartitionKey>(yourPartitionKey)`.");
            }

            var tableHolder = tableHolderResolver.ResolveAndSetIfAvailable(context.Extensions);

            var setAsDispatchedHolder = context.Extensions.Get<SetAsDispatchedHolder>();
            setAsDispatchedHolder.PartitionKey = partitionKey;
            setAsDispatchedHolder.TableHolder = tableHolder;

            IOutboxTransaction.PartitionKey = partitionKey;
            IOutboxTransaction.StorageSession.TableHolder = tableHolder;

            var outboxRecord = await tableHolder.Table.ReadOutboxRecord(context.MessageId, IOutboxTransaction.PartitionKey.Value, context.Extensions, context.CancellationToken)
                .ConfigureAwait(false);

            if (outboxRecord is null)
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            setAsDispatchedHolder.Record = outboxRecord;

            // Signals that Outbox persister Store and Commit should be no-ops
            IOutboxTransaction.SuppressStoreAndCommit = true;

            var pendingTransportOperations = context.Extensions.Get<PendingTransportOperations>();
            setter(pendingTransportOperations);

            foreach (var operation in outboxRecord.Operations)
            {
                var message = new OutgoingMessage(operation.MessageId, operation.Headers, operation.Body);

                pendingTransportOperations.Add(
                    new TransportOperation(
                        message,
                        DeserializeRoutingStrategy(operation.Options),
                        operation.Options,
                        DispatchConsistency.Isolated));
            }
        }

        static AddressTag DeserializeRoutingStrategy(Dictionary<string, string> options)
        {
            if (options.TryGetValue("Destination", out var destination))
            {
                return new UnicastAddressTag(destination);
            }

            if (options.TryGetValue("EventType", out var eventType))
            {
                return new MulticastAddressTag(Type.GetType(eventType, true));
            }

            throw new Exception("Could not find routing strategy to deserialize.");
        }

        static Action<PendingTransportOperations> setter;
        readonly TableHolderResolver tableHolderResolver;
    }
}