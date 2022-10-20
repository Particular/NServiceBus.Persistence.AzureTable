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
                if (!entity.ContainsKey(accessor.Name))
                {
                    continue;
                }

                var value = entity[accessor.Name];
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
                    accessor.Setter(toCreate, entity.GetString(nameof(accessor.Name)));
                }
                else
                {
                    // We assume we have a specific type and will try to deserialize
                    // TODO: a scenario was removed here, review!
                    try
                    {
                        string propertyValue = entity.GetString(nameof(accessor.Name));
                        using (var reader = new StringReader(propertyValue))
                        using (var jsonReader = readerCreator(reader))
                        {
                            var deserialized = jsonSerializer.Deserialize(jsonReader, type);
                            accessor.Setter(toCreate, deserialized);
                        }
                    }
                    catch (Exception)
                    {
                        throw new NotSupportedException($"The property type '{type.Name}' is not supported in Azure Table Storage and it cannot be deserialized with JSON.NET.");
                    }
                }
            }
            return toCreate;
        }

        public static TableEntity ToTableEntity(object sagaData, TableEntity toPersist, JsonSerializer jsonSerializer,
                                                Func<TextWriter, JsonWriter> writerCreator)
        {
            foreach (var accessor in GetPropertyAccessors(sagaData.GetType()))
            {
                var name = accessor.Name;
                var type = accessor.PropertyType;
                var value = accessor.Getter(sagaData);

                if (type == typeof(DateTime))
                {
                    if (TryGetNullable(type, value, out DateTime? dateTime))
                    {
                        if (!dateTime.HasValue || dateTime.Value < StorageTableMinDateTime)
                        {
                            throw new Exception(
                                $"Saga data of type '{sagaData.GetType().FullName}' with DateTime property '{name}' has an invalid value '{dateTime}'. Value cannot be null and must be equal to or greater than '{StorageTableMinDateTime}'.");
                        }

                        toPersist.Add(name, value);
                    }
                }
                else if (!type.IsPrimitive && type != typeof(byte[]) && type != typeof(DateTime))
                {
                    using (var stringWriter = new StringWriter())
                    using (var writer = writerCreator(stringWriter))
                    {
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
                else
                {
                    toPersist.Add(name, value);
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

        static bool TrySetNullable(TableEntity tableEntity, object entity, PropertyAccessor setter)
        {
            return
                TrySetNullable<bool>(tableEntity, entity, setter) ||
                TrySetNullable<DateTime>(tableEntity, entity, setter) ||
                TrySetNullable<Guid>(tableEntity, entity, setter) ||
                TrySetNullable<int>(tableEntity, entity, setter) ||
                TrySetNullable<double>(tableEntity, entity, setter) ||
                TrySetNullable<long>(tableEntity, entity, setter);
        }

        static bool TrySetNullable<TPrimitive>(TableEntity tableEntity, object entity, PropertyAccessor setter)
            where TPrimitive : struct
        {
            if (setter.PropertyType == typeof(TPrimitive))
            {
                if (tableEntity.ContainsKey(nameof(setter.Name)))
                {
                    var value = (TPrimitive?)tableEntity[nameof(setter.Name)];
                    var nonNullableValue = value ?? default;
                    setter.Setter(entity, nonNullableValue);
                    return true;
                }

                setter.Setter(entity, default);
                return true;
            }

            if (setter.PropertyType == typeof(TPrimitive?))
            {
                if (tableEntity.ContainsKey(nameof(setter.Name)))
                {
                    var value = (TPrimitive?)tableEntity[nameof(setter.Name)];
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

            if (!IsSupportedPropertyType(propertyInfo.PropertyType))
            {
                throw new NotSupportedException($"The property type '{propertyInfo.PropertyType.Name}' is not supported in Azure Table Storage");

            }

            return $"{propertyInfo.Name} eq {correlationProperty.Value}";
        }

        static bool IsSupportedPropertyType(Type propertyType) =>
            propertyType == typeof(byte[]) ||
            propertyType == typeof(bool) ||
            propertyType == typeof(DateTime) ||
            propertyType == typeof(Guid) ||
            propertyType == typeof(int) ||
            propertyType == typeof(long) ||
            propertyType == typeof(double) ||
            propertyType == typeof(string);

        static readonly ConcurrentDictionary<Type, IReadOnlyCollection<PropertyAccessor>> propertyAccessorCache = new();

        static readonly DateTime StorageTableMinDateTime = new DateTime(1601, 1, 1);

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