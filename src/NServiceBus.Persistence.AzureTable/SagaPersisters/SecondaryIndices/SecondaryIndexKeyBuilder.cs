namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.IO;
    using Newtonsoft.Json;
    using Sagas;

    static class SecondaryIndexKeyBuilder
    {
        public static PartitionRowKeyTuple BuildTableKey<TSagaData>(SagaCorrelationProperty correlationProperty)
            where TSagaData : IContainSagaData
        {
            var sagaDataTypeName = typeof(TSagaData).FullName;
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