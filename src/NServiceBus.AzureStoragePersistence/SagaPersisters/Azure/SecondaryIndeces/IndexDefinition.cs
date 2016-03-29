namespace NServiceBus.SagaPersisters.Azure.SecondaryIndeces
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Reflection;
    using Newtonsoft.Json;
    using NServiceBus.Sagas;

    class IndexDefinition
    {
        static ConcurrentDictionary<Type, IndexDefinition> sagaToIndex = new ConcurrentDictionary<Type, IndexDefinition>();
        static IndexDefinition NullValue = new IndexDefinition();
        
        string sagaDataTypeName;
        string propertyName;

        IndexDefinition()
        {
        }

        IndexDefinition(Type sagaDataType, PropertyInfo pi)
        {
            sagaDataTypeName = sagaDataType.FullName;
            propertyName = pi.Name;
        }

        public static IndexDefinition Get(Type sagaDataType, SagaCorrelationProperty correlationProperty)
        {
            var index = sagaToIndex.GetOrAdd(sagaDataType, type =>
            {
                if (correlationProperty == null)
                {
                    return NullValue;
                }

                return new IndexDefinition(sagaDataType, sagaDataType.GetProperty(correlationProperty.Name));
            });

            if (ReferenceEquals(index, NullValue))
            {
                return null;
            }

            return index;
        }

        public void ValidateProperty(string propertyName)
        {
            if (this.propertyName != propertyName)
            {
                throw new ArgumentException($"The following saga '{sagaDataTypeName}' is not indexed by '{propertyName}'. The only secondary index is defined for '{this.propertyName}'. " +
                                            $"Ensure that the saga is correlated properly.");
            }
        }

        public PartitionRowKeyTuple BuildTableKey(object propertyValue)
        {
            return new PartitionRowKeyTuple($"Index_{sagaDataTypeName}_{propertyName}_{Serialize(propertyValue)}", "");
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