namespace NServiceBus.Persistence.AzureStorage
{
    using System;
    using System.Collections.Generic;
    using Outbox;
    using Newtonsoft.Json;
    using Microsoft.Azure.Cosmos.Table;

    class OutboxRecord : TableEntity
    {
        [IgnoreProperty]
        public string Id
        {
            get => RowKey;
            set => RowKey = value;
        }

        public bool Dispatched { get; set; }

        public string TransportOperations { get; set; }

        public string DispatchedAt { get; set; }

        [IgnoreProperty]
        public TransportOperation[] Operations { get; set; } = Array.Empty<TransportOperation>();

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            TransportOperations = JsonConvert.SerializeObject(Operations, Formatting.Indented);
            return base.WriteEntity(operationContext);
        }

        public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            base.ReadEntity(properties, operationContext);
            Operations = JsonConvert.DeserializeObject<TransportOperation[]>(TransportOperations);
        }

        public void SetAsDispatched()
        {
            Dispatched = true;
            Operations = Array.Empty<TransportOperation>();
            DispatchedAt = DateTimeOffsetHelper.ToWireFormattedString(DateTimeOffset.UtcNow);
        }
    }
}