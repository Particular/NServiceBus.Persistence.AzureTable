namespace NServiceBus.SagaPersisters.Azure.SecondaryIndeces
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq.Expressions;
    using System.Reflection;
    using Newtonsoft.Json;
    using NServiceBus.Saga;

    public sealed class IndexDefintion
    {
        static readonly ConcurrentDictionary<Type, object> sagaToIndex = new ConcurrentDictionary<Type, object>();
        static readonly object NullValue = new object();

        static readonly ParameterExpression ObjectParameter = Expression.Parameter(typeof(object));
        readonly string propertyName;

        readonly string sagaTypeName;

        private IndexDefintion(Type sagaType, PropertyInfo pi)
        {
            sagaTypeName = sagaType.FullName;
            propertyName = pi.Name;
            Accessor = Expression.Lambda<Func<object, object>>(Expression.Convert(Expression.MakeMemberAccess(Expression.Convert(ObjectParameter, sagaType), pi), typeof(object)), ObjectParameter).Compile();
        }

        public Func<object, object> Accessor { get; }

        public static IndexDefintion Get(Type sagaType)
        {
            var index = sagaToIndex.GetOrAdd(sagaType, type =>
            {
                var pi = UniqueAttribute.GetUniqueProperty(sagaType);
                if (pi == null)
                {
                    return NullValue;
                }

                return new IndexDefintion(sagaType, pi);
            });

            if (ReferenceEquals(index, NullValue))
            {
                return null;
            }

            return (IndexDefintion) index;
        }

        public void ValidateProperty(string propertyName)
        {
            if (this.propertyName != propertyName)
            {
                throw new ArgumentException($"The following saga '{sagaTypeName}' is not indexed by '{propertyName}'. The only secondary index is defined for '{this.propertyName}'. " +
                                            $"Ensure that the saga is correlated properly.");
            }
        }

        public PartitionRowKeyTuple BuildTableKey(object propertyValue)
        {
            return new PartitionRowKeyTuple($"Index_{sagaTypeName}_{propertyName}_{Serialize(propertyValue)}", "");
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