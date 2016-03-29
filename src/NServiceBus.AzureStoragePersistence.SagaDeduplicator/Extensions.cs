namespace NServiceBus.AzureStoragePersistence.SagaDeduplicator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.WindowsAzure.Storage.Table;

    public static class Extensions
    {
        public static EdmType GetPropertyType(this CloudTable table, string propertyName)
        {
            var query = new TableQuery
            {
                TakeCount = 1
            };
            EntityProperty propertyValue;
            var entity = table.ExecuteQuery(query).SingleOrDefault();
            if (entity == null)
            {
                throw new ArgumentException($"There are not entities in the table '{table.Name}'. Ensure that a proper table is selected.");
            }

            if (entity.Properties.TryGetValue(propertyName, out propertyValue) == false)
            {
                throw new KeyNotFoundException($"The property {propertyName} is not present in table {table.Name}. Ensure that you selected an existing property of {table.Name}.");
            }

            return propertyValue.PropertyType;
        }
    }
}