namespace NServiceBus.Persistence.AzureStorage
{
    using System;
    using System.IO;
    using Newtonsoft.Json;
    using Sagas;

    static class SecondaryIndexKeyBuilder
    {
        public static PartitionRowKeyTuple BuildTableKey(Type sagaType, SagaCorrelationProperty correlationProperty)
        {
            var sagaDataTypeName = sagaType.FullName;
            var partitionKey = $"Index_{sagaDataTypeName}_{correlationProperty.Name}_{Serialize(correlationProperty.Value)}";
            return new PartitionRowKeyTuple(partitionKey, string.Empty);
        }

        static string Serialize(object propertyValue)
        {
            using (var writer = new StringWriter())
            {
                jsonSerializer.Serialize(writer, propertyValue);
                writer.Flush();
                return writer.ToString();
            }
        }

        static JsonSerializer jsonSerializer = new JsonSerializer();
    }
}