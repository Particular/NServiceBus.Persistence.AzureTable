namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Azure;
    using Azure.Data.Tables;
    using Newtonsoft.Json;
    using Outbox;

    class OutboxRecord : ITableEntity
    {
        static ReadOnlyMemoryConverter ReadOnlyMemoryConverter = new();

        // ignoring this property to avoid double storing and clashing with Cosmos Id property.
        public string Id
        {
            get => RowKey;
            set => RowKey = value;
        }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public bool Dispatched { get; set; }

        public string TransportOperations
        {
            get => SerializeTransportOperations(Operations);
            set => DeserializeAndSetTransportOperations(value);
        }

        public string DispatchedAt { get; set; }

        public TransportOperation[] Operations { get; set; } = Array.Empty<TransportOperation>();

        public void SetAsDispatched()
        {
            Dispatched = true;
            Operations = Array.Empty<TransportOperation>();
            DispatchedAt = DateTimeOffsetHelper.ToWireFormattedString(DateTimeOffset.UtcNow);
        }

        internal void DeserializeAndSetTransportOperations(string transportOperations)
        {
            var storageOperations = JsonConvert.DeserializeObject<StorageTransportOperation[]>(transportOperations, ReadOnlyMemoryConverter);
            Operations = storageOperations.Select(op =>
                                              new TransportOperation(op.MessageId, new Transport.DispatchProperties(op.Options), op.Body,
                                                  op.Headers))
                                          .ToArray();
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

        internal class StorageTransportOperation
        {
            public string MessageId { get; set; }
            public Dictionary<string, string> Options { get; set; }
            public ReadOnlyMemory<byte> Body { get; set; }
            public Dictionary<string, string> Headers { get; set; }
        }
    }
}