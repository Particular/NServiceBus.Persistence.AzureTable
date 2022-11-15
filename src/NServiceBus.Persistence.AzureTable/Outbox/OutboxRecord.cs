namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using Azure;
    using Azure.Data.Tables;
    using Newtonsoft.Json;
    using Outbox;

    sealed class OutboxRecord : ITableEntity
    {
        // ignoring this property to avoid double storing and clashing with Cosmos Id property.
        [IgnoreDataMember]
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

        /// <summary>
        /// This property is assumed to be only ever accessed by the serialization mechanism of the SDK
        /// </summary>
        public string TransportOperations
        {
            get => SerializeTransportOperations(Operations);
            set => Operations = DeserializeTransportOperations(value);
        }

        public string DispatchedAt { get; set; }

        [IgnoreDataMember]
        public TransportOperation[] Operations { get; set; } = Array.Empty<TransportOperation>();

        public void SetAsDispatched()
        {
            Dispatched = true;
            Operations = Array.Empty<TransportOperation>();
            DispatchedAt = DateTimeOffsetHelper.ToWireFormattedString(DateTimeOffset.UtcNow);
        }

        TransportOperation[] DeserializeTransportOperations(string transportOperations)
        {
            var storageOperations = DeserializeStorageTransportOperations(transportOperations);
            var operations = new TransportOperation[storageOperations.Length];
            int index = 0;
            foreach (var storageOperation in storageOperations)
            {
                operations[index] = new TransportOperation(
                    storageOperation.MessageId,
                    new Transport.DispatchProperties(storageOperation.Options), storageOperation.Body,
                    storageOperation.Headers);
                index++;
            }
            return operations;
        }

        public static StorageTransportOperation[] DeserializeStorageTransportOperations(string operations)
            => JsonConvert.DeserializeObject<StorageTransportOperation[]>(operations, Converters);

        static string SerializeTransportOperations(TransportOperation[] transportOperations)
        {
            var storageOperations = new StorageTransportOperation[transportOperations.Length];
            int index = 0;
            foreach (var transportOperation in transportOperations)
            {
                storageOperations[index] = new StorageTransportOperation
                {
                    MessageId = transportOperation.MessageId,
                    Body = transportOperation.Body,
                    Options = transportOperation.Options,
                    Headers = transportOperation.Headers
                };
                index++;
            }
            return JsonConvert.SerializeObject(storageOperations, Formatting.Indented, Converters);
        }

        static readonly JsonConverter[] Converters = { new ReadOnlyMemoryConverter() };

        internal class StorageTransportOperation
        {
            public string MessageId { get; set; }
            public Dictionary<string, string> Options { get; set; }
            public ReadOnlyMemory<byte> Body { get; set; }
            public Dictionary<string, string> Headers { get; set; }
        }
    }
}