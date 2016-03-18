namespace NServiceBus.SagaPersisters.Azure.SecondaryIndeces
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq.Expressions;
    using System.Reflection;
    using Newtonsoft.Json;
    using NServiceBus.Sagas;

    public sealed class IndexDefinition
    {
        static readonly ConcurrentDictionary<Type, IndexDefinition> sagaToIndex = new ConcurrentDictionary<Type, IndexDefinition>();
        static readonly IndexDefinition NullValue = new IndexDefinition();

        static readonly ParameterExpression ObjectParameter = Expression.Parameter(typeof(object));
        readonly string propertyName;

        readonly string sagaDataTypeName;

        private IndexDefinition()
        {
        }

        private IndexDefinition(Type sagaDataType, PropertyInfo pi)
        {
            sagaDataTypeName = sagaDataType.FullName;
            propertyName = pi.Name;
            Accessor = Expression.Lambda<Func<object, object>>(Expression.Convert(Expression.MakeMemberAccess(Expression.Convert(ObjectParameter, sagaDataType), pi), typeof(object)), ObjectParameter).Compile();
        }

        public Func<object, object> Accessor { get; }

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

        private static string Serialize(object propertyValue)
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