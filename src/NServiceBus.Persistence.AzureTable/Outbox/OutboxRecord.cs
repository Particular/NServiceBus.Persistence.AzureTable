﻿namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Azure.Cosmos.Table;
    using Newtonsoft.Json;
    using Outbox;

    class OutboxRecord : TableEntity
    {
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
        [IgnoreProperty]
        public TransportOperation[] Operations { get; set; } = Array.Empty<TransportOperation>();

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            TransportOperations = JsonConvert.SerializeObject(
                Operations.Select(transportOperation => new StorageTransportOperation()
                {
                    MessageId = transportOperation.MessageId,
                    Body = transportOperation.Body.ToArray(),
                    Options = transportOperation.Options,
                    Headers = transportOperation.Headers
                }), Formatting.Indented);
            return base.WriteEntity(operationContext);
        }

        public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            base.ReadEntity(properties, operationContext);
            var storageOperations = JsonConvert.DeserializeObject<StorageTransportOperation[]>(TransportOperations);
            Operations = storageOperations.Select(op => new TransportOperation(op.MessageId, new Transport.DispatchProperties(op.Options), op.Body, op.Headers)).ToArray();
        }

        public void SetAsDispatched()
        {
            Dispatched = true;
            Operations = Array.Empty<TransportOperation>();
            DispatchedAt = DateTimeOffsetHelper.ToWireFormattedString(DateTimeOffset.UtcNow);
        }

        class StorageTransportOperation
        {
            public string MessageId { get; set; }
            public Dictionary<string, string> Options { get; set; }
            public byte[] Body { get; set; }
            public Dictionary<string, string> Headers { get; set; }
        }
    }
}