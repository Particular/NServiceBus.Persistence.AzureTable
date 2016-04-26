namespace NServiceBus.Persistence.AzureStorage.SecondaryIndices
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
            return new PartitionRowKeyTuple($"Index_{sagaDataTypeName}_{correlationProperty.Name}_{Serialize(correlationProperty.Value)}", "");
        }

        static string Serialize(object propertyValue)
        {
            using (var writer = new StringWriter())
            {
                new JsonSerializer().Serialize(writer, propertyValue);
                writer.Flush();
                return writer.ToString();
            }
        }
    }
}