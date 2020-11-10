namespace NServiceBus.Persistence.AzureStorage
{
    using System;
    using Microsoft.Azure.Cosmos.Table;

    // An entity holding information about the secondary index.
    class SecondaryIndexTableEntity : TableEntity
    {
        public Guid SagaId { get; set; }
    }
}