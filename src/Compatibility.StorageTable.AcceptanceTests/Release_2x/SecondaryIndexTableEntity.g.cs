namespace NServiceBus.Persistence.AzureTable.Release_2x
{
    using System;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// This is a copy of the saga persister code 2.4.1
    /// </summary>
    class SecondaryIndexTableEntity : TableEntity
    {
        public Guid SagaId { get; set; }

        public byte[] InitialSagaData { get; set; }
    }
}