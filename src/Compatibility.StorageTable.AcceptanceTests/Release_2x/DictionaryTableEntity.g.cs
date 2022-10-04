namespace NServiceBus.Persistence.AzureTable.Release_2x
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Azure.Data.Tables;


    /// <summary>
    /// This is a copy of the saga persister code 2.4.1
    /// </summary>
    class DictionaryTableEntity : TableEntity, IDictionary<string, EntityProperty>
    {
        public DictionaryTableEntity()
        {
            properties = new Dictionary<string, EntityProperty>();
            TableEntity.
        }

        public void Add(string key, EntityProperty value)
        {
            properties.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return properties.ContainsKey(key);
        }

        public ICollection<string> Keys => properties.Keys;

        public bool Remove(string key)
        {
            return properties.Remove(key);
        }

        public bool TryGetValue(string key, out EntityProperty value)
        {
            return properties.TryGetValue(key, out value);
        }

        public ICollection<EntityProperty> Values => properties.Values;

        public EntityProperty this[string key]
        {
            get { return properties[key]; }
            set { properties[key] = value; }
        }

        public void Add(KeyValuePair<string, EntityProperty> item)
        {
            properties.Add(item);
        }

        public void Clear()
        {
            properties.Clear();
        }

        public bool Contains(KeyValuePair<string, EntityProperty> item)
        {
            return properties.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, EntityProperty>[] array, int arrayIndex)
        {
            properties.CopyTo(array, arrayIndex);
        }

        public int Count => properties.Count;

        public bool IsReadOnly => properties.IsReadOnly;

        public bool Remove(KeyValuePair<string, EntityProperty> item)
        {
            return properties.Remove(item);
        }

        public IEnumerator<KeyValuePair<string, EntityProperty>> GetEnumerator()
        {
            return properties.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return properties.GetEnumerator();
        }

        public void ReadEntity(IDictionary<string, EntityProperty> entityProperties, OperationContext operationContext)
        {
            properties = entityProperties;
        }

        public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            return properties;
        }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string ETag { get; set; }

        public void Add(string key, bool value)
        {
            properties.Add(key, new EntityProperty(value));
        }

        public void Add(string key, byte[] value)
        {
            properties.Add(key, new EntityProperty(value));
        }

        public void Add(string key, DateTime? value)
        {
            properties.Add(key, new EntityProperty(value));
        }

        public void Add(string key, DateTimeOffset? value)
        {
            properties.Add(key, new EntityProperty(value));
        }

        public void Add(string key, double value)
        {
            properties.Add(key, new EntityProperty(value));
        }

        public void Add(string key, Guid value)
        {
            properties.Add(key, new EntityProperty(value));
        }

        public void Add(string key, int value)
        {
            properties.Add(key, new EntityProperty(value));
        }

        public void Add(string key, long value)
        {
            properties.Add(key, new EntityProperty(value));
        }

        public void Add(string key, string value)
        {
            properties.Add(key, new EntityProperty(value));
        }

        IDictionary<string, EntityProperty> properties;
    }
}

namespace NServiceBus.Persistence.AzureStorage
{
    using Newtonsoft.Json;
}