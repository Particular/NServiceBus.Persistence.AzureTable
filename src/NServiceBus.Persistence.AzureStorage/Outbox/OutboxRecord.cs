namespace NServiceBus.Persistence.AzureStorage
{
    using System;
    using Microsoft.Azure.Cosmos.Table;

    class OutboxRecord : TableEntity
    {
        public string Id { get; set; }

        public bool Dispatched { get; set; }

        public byte[] TransportOperations { get; set; } = Array.Empty<byte>();
    }
}