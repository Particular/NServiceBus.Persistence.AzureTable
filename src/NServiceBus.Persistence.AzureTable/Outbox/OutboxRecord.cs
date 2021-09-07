namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Azure.Cosmos.Table;
    using Newtonsoft.Json;
    using Outbox;

    class OutboxRecord : TableEntity
    {
        static ReadOnlyMemoryConverter ReadOnlyMemoryConverter = new ReadOnlyMemoryConverter();

        // ignoring this property to avoid double storing and clashing with Cosmos Id property.
        [IgnoreProperty]
        public string Id
        {
            get => RowKey;
            set => RowKey = value;
        }

        public bool Dispatched { get; set; }

        public string TransportOperations { get; set; }

        public string DispatchedAt { get; set; }

        // ignoring this property because we are custom serializing the operations into TransportOperations and deserializing it back
        [IgnoreProperty] public TransportOperation[] Operations { get; set; } = Array.Empty<TransportOperation>();

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            TransportOperations = SerializeTransportOperations(Operations);
            return base.WriteEntity(operationContext);
        }

        internal static StorageTransportOperation[] DeserializeTransportOperations(string transportOperations)
        {
            return JsonConvert.DeserializeObject<StorageTransportOperation[]>(transportOperations, ReadOnlyMemoryConverter);
        }

        internal static string SerializeTransportOperations(TransportOperation[] transportOperations)
        {
            return JsonConvert.SerializeObject(
                transportOperations.Select(transportOperation => new StorageTransportOperation()
                {
                    MessageId = transportOperation.MessageId,
                    Body = transportOperation.Body,
                    Options = transportOperation.Options,
                    Headers = transportOperation.Headers
                }), Formatting.Indented, ReadOnlyMemoryConverter);
        }

        public override void ReadEntity(IDictionary<string, EntityProperty> properties,
            OperationContext operationContext)
        {
            base.ReadEntity(properties, operationContext);
            var storageOperations = DeserializeTransportOperations(TransportOperations);
            Operations = storageOperations.Select(op =>
                    new TransportOperation(op.MessageId, new Transport.DispatchProperties(op.Options), op.Body,
                        op.Headers))
                .ToArray();
        }

        public void SetAsDispatched()
        {
            Dispatched = true;
            Operations = Array.Empty<TransportOperation>();
            DispatchedAt = DateTimeOffsetHelper.ToWireFormattedString(DateTimeOffset.UtcNow);
        }

        internal class StorageTransportOperation
        {
            public string MessageId { get; set; }
            public Dictionary<string, string> Options { get; set; }
            public ReadOnlyMemory<byte> Body { get; set; }
            public Dictionary<string, string> Headers { get; set; }
        }
    }
}