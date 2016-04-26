namespace NServiceBus.Persistence.AzureStorage.SecondaryIndices
{
    using System;
    using Microsoft.WindowsAzure.Storage.Table;

    // An entity holding information about the secondary index.
    class SecondaryIndexTableEntity : TableEntity
    {
        public Guid SagaId { get; set; }

        public byte[] InitialSagaData { get; set; }
    }
}