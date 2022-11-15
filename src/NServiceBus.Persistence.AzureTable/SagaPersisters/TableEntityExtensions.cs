namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Collections.Concurrent;
    using System.Linq.Expressions;
    using Azure.Data.Tables;
    using Newtonsoft.Json;
    using Sagas;

    static class TableEntityExtensions
    {
        public static TEntity ToSagaData<TEntity>(TableEntity entity, JsonSerializer jsonSerializer, Func<TextReader, JsonReader> readerCreator)
            where TEntity : IContainSagaData =>
            (TEntity)ToSagaData(typeof(TEntity), entity, jsonSerializer, readerCreator);

        static object ToSagaData(Type sagaDataType, TableEntity entity, JsonSerializer jsonSerializer, Func<TextReader, JsonReader> readerCreator)
        {
            var toCreate = Activator.CreateInstance(sagaDataType);
            foreach (var accessor in GetPropertyAccessors(sagaDataType))
            {
                if (accessor.Name == nameof(IContainSagaData.Id) && accessor.PropertyType == typeof(Guid))
                {
                    var sagaDataId = new Guid(entity.RowKey);
                    accessor.Setter(toCreate, sagaDataId);
                    continue;
                }

                if (!entity.ContainsKey(accessor.Name))
                {
                    continue;
                }

                var type = accessor.PropertyType;

                if (type == typeof(byte[]))
                {
                    var binaryValue = entity.GetBinary(accessor.Name);
                    accessor.Setter(toCreate, binaryValue);
                }
                else if (TrySetNullable(entity, toCreate, accessor))
                {
                }
                else if (type == typeof(string))
                {
                    accessor.Setter(toCreate, entity.GetString(accessor.Name));
                }
                else
                {
                    // We assume we have a specific type and will try to deserialize
                    try
                    {
                        string propertyValue = entity.GetString(accessor.Name);
                        using var reader = new StringReader(propertyValue);
                        using var jsonReader = readerCreator(reader);
                        var deserialized = jsonSerializer.Deserialize(jsonReader, type);
                        accessor.Setter(toCreate, deserialized);
                    }
                    catch (Exception)
                    {
                        throw new NotSupportedException($"The property type '{type.Name}' is not supported in Azure Table Storage and it cannot be deserialized with JSON.NET.");
                    }
                }
            }
            return toCreate;
        }

        public static TableEntity ToTableEntity(IContainSagaData sagaData, TableEntity toPersist, JsonSerializer jsonSerializer,
                                                Func<TextWriter, JsonWriter> writerCreator)
        {
            foreach (var accessor in GetPropertyAccessors(sagaData.GetType()))
            {
                if (accessor.Name == nameof(IContainSagaData.Id))
                {
                    continue;
                }

                var name = accessor.Name;
                var type = accessor.PropertyType;
                var value = accessor.Getter(sagaData);

                // TODO: look into datetimeoffset
                if (type == typeof(byte[]) ||
                    type == typeof(string) ||
                    TryGetNullable(type, value, out bool? _) ||
                    TryGetNullable(type, value, out Guid? _) ||
                    TryGetNullable(type, value, out int? _) ||
                    TryGetNullable(type, value, out long? _) ||
                    TryGetNullable(type, value, out double? _))
                {
                    toPersist.Add(name, value);
                }
                else if (TryGetNullable(type, value, out DateTime? dateTime))
                {
                    if (!dateTime.HasValue || dateTime.Value < StorageTableMinDateTime)
                    {
                        throw new Exception(
                            $"Saga data of type '{sagaData.GetType().FullName}' with DateTime property '{name}' has an invalid value '{dateTime}'. Value cannot be null and must be equal to or greater than '{StorageTableMinDateTime}'.");
                    }

                    toPersist.Add(name, value);
                }
                else
                {
                    using var stringWriter = new StringWriter();
                    using var writer = writerCreator(stringWriter);
                    try
                    {
                        jsonSerializer.Serialize(writer, value, type);
                    }
                    catch (Exception)
                    {
                        throw new NotSupportedException(
                            $"The property type '{type.Name}' is not supported in Azure Table Storage and it cannot be serialized with JSON.NET.");
                    }

                    toPersist[name] = stringWriter.ToString();
                }
            }

            return toPersist;
        }

        static IReadOnlyCollection<PropertyAccessor> GetPropertyAccessors(Type sagaDataType)
        {
            var accessors = propertyAccessorCache.GetOrAdd(sagaDataType, dataType =>
            {
                var setters = new List<PropertyAccessor>();
                var entityProperties = dataType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var propertyInfo in entityProperties)
                {
                    setters.Add(new PropertyAccessor(propertyInfo));
                }
                return setters;
            });
            return accessors;
        }

        static bool TryGetNullable<TPrimitive>(Type type, object value, out TPrimitive? nullable)
            where TPrimitive : struct
        {
            if (type == typeof(TPrimitive))
            {
                nullable = (TPrimitive)value;
                return true;
            }

            if (type == typeof(TPrimitive?))
            {
                nullable = (TPrimitive?)value;
                return true;
            }

            nullable = null;
            return false;
        }

        static bool TrySetNullable(TableEntity tableEntity, object entity, PropertyAccessor setter) =>
            TrySetNullable<bool>(tableEntity, entity, setter) ||
            TrySetNullableDateTime(tableEntity, entity, setter) ||
            TrySetNullable<Guid>(tableEntity, entity, setter) ||
            TrySetNullable<int>(tableEntity, entity, setter) ||
            TrySetNullable<double>(tableEntity, entity, setter) ||
            TrySetNullable<long>(tableEntity, entity, setter);

        static bool TrySetNullableDateTime(TableEntity tableEntity, object entity, PropertyAccessor setter)
        {
            if (setter.PropertyType == typeof(DateTime))
            {
                SetDateTime(tableEntity, entity, setter, false);
                return true;
            }

            if (setter.PropertyType == typeof(DateTime?))
            {
                SetDateTime(tableEntity, entity, setter, true);
                return true;
            }

            return false;
        }

        static void SetDateTime(TableEntity tableEntity, object entity, PropertyAccessor setter, bool allowNull)
        {
            DateTime? value = default;

            if (tableEntity.ContainsKey(setter.Name))
            {
                value = tableEntity[setter.Name] switch
                {
                    DateTimeOffset dateTimeOffsetValue => dateTimeOffsetValue.UtcDateTime,
                    _ => (DateTime?)tableEntity[setter.Name]
                };
            }

            setter.Setter(entity, allowNull ? value : value ?? default);
        }

        static bool TrySetNullable<TPrimitive>(TableEntity tableEntity, object entity, PropertyAccessor setter)
            where TPrimitive : struct
        {
            if (setter.PropertyType == typeof(TPrimitive))
            {
                if (tableEntity.ContainsKey(setter.Name))
                {
                    var value = (TPrimitive?)tableEntity[setter.Name];
                    var nonNullableValue = value ?? default;
                    setter.Setter(entity, nonNullableValue);
                    return true;
                }

                setter.Setter(entity, default);
                return true;
            }

            if (setter.PropertyType == typeof(TPrimitive?))
            {
                if (tableEntity.ContainsKey(setter.Name))
                {
                    var value = (TPrimitive?)tableEntity[setter.Name];
                    setter.Setter(entity, value);
                    return true;
                }

                setter.Setter(entity, default);
                return true;
            }

            return false;
        }

        public static string BuildWherePropertyQuery<TSagaData>(SagaCorrelationProperty correlationProperty)
            where TSagaData : IContainSagaData
        {
            var propertyInfo = typeof(TSagaData).GetProperty(correlationProperty.Name);
            if (propertyInfo == null)
            {
                return null;
            }

            try
            {
                // We're doing this in two phases to avoid the CreateQueryFilter API from escaping the propertyInfo.Name value as it would be any argument
                var propertyValue = TableClient.CreateQueryFilter($"{correlationProperty.Value}");
                return $"{propertyInfo.Name} eq {propertyValue}";
            }
            catch (ArgumentException exception)
            {
                throw new NotSupportedException($"The property type '{propertyInfo.PropertyType.Name}' is not supported in Azure Table Storage", exception);
            }
        }

        static readonly ConcurrentDictionary<Type, IReadOnlyCollection<PropertyAccessor>> propertyAccessorCache = new();

        static readonly DateTime StorageTableMinDateTime = new(1601, 1, 1);

        sealed class PropertyAccessor
        {
            public PropertyAccessor(PropertyInfo propertyInfo)
            {
                Setter = GenerateSetter(propertyInfo);
                Getter = GenerateGetter(propertyInfo);
                Name = propertyInfo.Name;
                PropertyType = propertyInfo.PropertyType;
            }

            static Func<object, object> GenerateGetter(PropertyInfo propertyInfo)
            {
                var instance = Expression.Parameter(typeof(object), "instance");
                var instanceCast = !propertyInfo.DeclaringType.IsValueType
                    ? Expression.TypeAs(instance, propertyInfo.DeclaringType)
                    : Expression.Convert(instance, propertyInfo.DeclaringType);
                var getter = Expression
                    .Lambda<Func<object, object>>(
                        Expression.TypeAs(Expression.Call(instanceCast, propertyInfo.GetGetMethod()), typeof(object)), instance)
                    .Compile();
                return getter;
            }

            static Action<object, object> GenerateSetter(PropertyInfo propertyInfo)
            {
                var instance = Expression.Parameter(typeof(object), "instance");
                var value = Expression.Parameter(typeof(object), "value");
                // value as T is slightly faster than (T)value, so if it's not a value type, use that
                var instanceCast = !propertyInfo.DeclaringType.IsValueType
                    ? Expression.TypeAs(instance, propertyInfo.DeclaringType)
                    : Expression.Convert(instance, propertyInfo.DeclaringType);
                var valueCast = !propertyInfo.PropertyType.IsValueType
                    ? Expression.TypeAs(value, propertyInfo.PropertyType)
                    : Expression.Convert(value, propertyInfo.PropertyType);
                var setter = Expression
                    .Lambda<Action<object, object>>(Expression.Call(instanceCast, propertyInfo.GetSetMethod(), valueCast), instance,
                        value).Compile();
                return setter;
            }

            public Action<object, object> Setter { get; }
            public Func<object, object> Getter { get; }
            public string Name { get; }
            public Type PropertyType { get; }
        }
    }
}