namespace NServiceBus.Persistence.AzureStorage
{
    using System;
    using System.Collections.Generic;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;

    class TimeoutManagerDataEntity : TableEntity
    {
        public TimeoutManagerDataEntity() { }

        public TimeoutManagerDataEntity(string partitionKey, string rowKey)
            : base(partitionKey, rowKey)
        {
        }

        public DateTime LastSuccessfulRead { get; set; }

        public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            base.ReadEntity(properties, operationContext);
            //fix typo
            if (!properties.Keys.Contains("LastSuccessfulRead") && properties.Keys.Contains("LastSuccessfullRead"))
            {
                this.LastSuccessfulRead = properties["LastSuccessfullRead"].DateTime.Value;
            }
        }
    }
}