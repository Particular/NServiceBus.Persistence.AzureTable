namespace NServiceBus.SagaPersisters.Azure.SecondaryIndeces
{
    using System;
    using System.IO;
    using Newtonsoft.Json;
    using NServiceBus.Sagas;

    public static class SecondaryIndexKeyBuilder
    {
        public static PartitionRowKeyTuple BuildTableKey(Type sagaType, SagaCorrelationProperty correlationProperty)
        {
            var sagaDataTypeName = sagaType.FullName;
            return new PartitionRowKeyTuple($"Index_{sagaDataTypeName}_{correlationProperty.Name}_{Serialize(correlationProperty.Value)}", "");
        }

        static string Serialize(object propertyValue)
        {
            using (var sw = new StringWriter())
            {
                new JsonSerializer().Serialize(sw, propertyValue);
                sw.Flush();
                return sw.ToString();
            }
        }
    }
}