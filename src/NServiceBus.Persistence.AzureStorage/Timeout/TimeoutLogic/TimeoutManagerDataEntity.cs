namespace NServiceBus.Persistence.AzureStorage
{
    using System;
    using Microsoft.WindowsAzure.Storage.Table;

    class TimeoutManagerDataEntity : TableEntity
    {
        public TimeoutManagerDataEntity() { }

        public TimeoutManagerDataEntity(string partitionKey, string rowKey)
            : base(partitionKey, rowKey)
        {
        }

        public DateTime LastSuccessfulRead { get; set; }

    }
}