namespace NServiceBus.Persistence.AzureStorage
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Azure.Cosmos.Table;

    class DictionaryTableEntity : TableEntity
    {
        public DictionaryTableEntity()
        {
            properties = new Dictionary<string, EntityProperty>();
        }

        public bool WillBeStoredOnPremium { private get; set; }
        public bool WasStoredOnPremium => !properties.ContainsKey("Id");

        public bool ContainsKey(string key)
        {
            return properties.ContainsKey(key);
        }

        public bool TryGetValue(string key, out EntityProperty value)
        {
            return properties.TryGetValue(key, out value);
        }

        public EntityProperty this[string key]
        {
            get { return properties[key]; }
            set { properties[key] = value; }
        }

        public override void ReadEntity(IDictionary<string, EntityProperty> entityProperties, OperationContext operationContext)
        {
            properties = entityProperties;
            if (WasStoredOnPremium)
            {
                properties["Id"] = new EntityProperty(Guid.Parse(RowKey));
            }
        }

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            if (WillBeStoredOnPremium)
            {
                properties.Remove("Id");
            }
            return properties;
        }

        IDictionary<string, EntityProperty> properties;
    }
}