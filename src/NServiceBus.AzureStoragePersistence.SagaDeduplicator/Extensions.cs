namespace NServiceBus.AzureStoragePersistence.SagaDeduplicator
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.WindowsAzure.Storage.Table;

    public static class Extensions
    {
        public static EdmType GetPropertyType(this CloudTable table, string propertyName)
        {
            var query = new TableQuery
            {
                SelectColumns = new[]
                {
                    propertyName
                }
            };

            TableContinuationToken token = null;
            do
            {
                var segment = table.ExecuteQuerySegmented(query, token);
                token = segment.ContinuationToken;
                var entityHavingProperty = segment.Results.FirstOrDefault(dte => dte.Properties.ContainsKey(propertyName));
                if (entityHavingProperty != null)
                {
                    return entityHavingProperty.Properties[propertyName].PropertyType;
                }
            } while (token != null);

            throw new KeyNotFoundException($"The property {propertyName} is not present in table {table.Name}. Ensure that you selected an existing property of {table.Name}.");
        }
    }
}