namespace NServiceBus.TransactionalSession
{
    using System;
    using System.Threading.Tasks;
    using Persistence.AzureTable;
    using Pipeline;

    sealed class AzureTableControlMessageBehavior : IBehavior<ITransportReceiveContext, ITransportReceiveContext>
    {
        public const string PartitionKeyStringHeaderKey = "NServiceBus.TxSession.AzureTable.PartitionKeyString";
        public const string TableInformationHeaderKey = "NServiceBus.TxSession.AzureTable.TableInformation";
        public const string OutboxEndpointNameHeaderKey = "NServiceBus.TxSession.AzureTable.OutboxEndpointName";

        public Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, Task> next)
        {
            if (context.Message.Headers.TryGetValue(PartitionKeyStringHeaderKey, out var partitionKeyString))
            {
                var partitionKeyInstance = new TableEntityPartitionKey(partitionKeyString);
                context.Extensions.Set(partitionKeyInstance);
            }

            if (context.Message.Headers.TryGetValue(TableInformationHeaderKey, out string tableName))
            {
                var tableInformationInstance = new TableInformation(tableName);
                context.Extensions.Set(tableInformationInstance);
            }

            if (context.Message.Headers.TryGetValue(OutboxEndpointNameHeaderKey, out var outboxEndpointName))
            {
                context.Extensions.Set(new OutboxSourceEndpointName(outboxEndpointName));
            }

            return next(context);
        }
    }
}