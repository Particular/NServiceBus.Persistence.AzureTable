namespace NServiceBus.Persistence.AzureTable
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

        [IgnoreProperty]
        public string Id
        {
            get => RowKey;
            set => RowKey = value;
        }

        [IgnoreProperty]
        public CloudTable Table { get; set; }

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
            get => properties[key];
            set => properties[key] = value;
        }

        public override void ReadEntity(IDictionary<string, EntityProperty> entityProperties, OperationContext operationContext)
        {
            properties = entityProperties;
            // in order to make sure the entity can be turned into IContainsSagaData we need to restore the ID property from the row key.
            properties["Id"] = EntityProperty.GeneratePropertyForGuid(Guid.Parse(RowKey));
        }

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            // The saga ID is already stored as the RowKey so there is no need to double store it.
            properties.Remove("Id");
            return properties;
        }

        IDictionary<string, EntityProperty> properties;
    }
}