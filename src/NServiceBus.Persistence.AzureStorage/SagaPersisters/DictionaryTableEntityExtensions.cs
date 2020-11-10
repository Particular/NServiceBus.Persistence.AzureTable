namespace NServiceBus.Persistence.AzureStorage
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using Microsoft.Azure.Cosmos.Table;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    static class DictionaryTableEntityExtensions
    {
        public static TEntity ToEntity<TEntity>(DictionaryTableEntity entity)
        {
            return (TEntity)ToEntity(typeof(TEntity), entity);
        }

        public static object ToEntity(Type entityType, DictionaryTableEntity entity)
        {
            var toCreate = Activator.CreateInstance(entityType);
            foreach (var propertyInfo in entityType.GetProperties())
            {
                if (entity.ContainsKey(propertyInfo.Name))
                {
                    var value = entity[propertyInfo.Name];
                    var type = propertyInfo.PropertyType;

                    if (type == typeof(byte[]))
                    {
                        propertyInfo.SetValue(toCreate, value.BinaryValue, null);
                    }
                    else if (TrySetNullable(value, toCreate, propertyInfo))
                    {
                    }
                    else if (type == typeof(string))
                    {
                        propertyInfo.SetValue(toCreate, value.StringValue, null);
                    }
                    else
                    {
                        if (value.PropertyType == EdmType.String)
                        {
                            // possibly serialized JSON.NET value
                            try
                            {
                                using (var stringReader = new StringReader(value.StringValue))
                                {
                                    var deserialized = jsonSerializer.Deserialize(stringReader, type);
                                    propertyInfo.SetValue(toCreate, deserialized, null);
                                }
                            }
                            catch (Exception)
                            {
                                throw new NotSupportedException($"The property type '{type.Name}' is not supported in Windows Azure Table Storage and it cannot be deserialized with JSON.NET.");
                            }
                        }
                        else
                        {
                            throw new NotSupportedException($"The property type '{type.Name}' is not supported in Windows Azure Table Storage");
                        }
                    }
                }
            }
            return toCreate;
        }

        public static DictionaryTableEntity ToDictionaryTableEntity(object entity, DictionaryTableEntity toPersist, IEnumerable<PropertyInfo> properties)
        {
            foreach (var propertyInfo in properties)
            {
                var name = propertyInfo.Name;
                var type = propertyInfo.PropertyType;
                var value = propertyInfo.GetValue(entity, null);

                if (type == typeof(byte[]))
                {
                    toPersist[name] = new EntityProperty((byte[])value);
                }
                else if (TryGetNullable(type, value, out bool? @bool))
                {
                    toPersist[name] = new EntityProperty(@bool);
                }
                else if (TryGetNullable(type, value, out DateTime? dateTime))
                {
                    if (!dateTime.HasValue || dateTime.Value < StorageTableMinDateTime)
                    {
                        throw new Exception($"Saga data of type '{entity.GetType().FullName}' with DateTime property '{name}' has an invalid value '{dateTime}'. Value cannot be null and must be equal to or greater than '{StorageTableMinDateTime}'.");
                    }

                    toPersist[name] = new EntityProperty(dateTime);
                }
                else if (TryGetNullable(type, value, out Guid? guid))
                {
                    toPersist[name] = new EntityProperty(guid);
                }
                else if (TryGetNullable(type, value, out int? @int))
                {
                    toPersist[name] = new EntityProperty(@int);
                }
                else if (TryGetNullable(type, value, out long? @long))
                {
                    toPersist[name] = new EntityProperty(@long);
                }
                else if (TryGetNullable(type, value, out double? @double))
                {
                    toPersist[name] = new EntityProperty(@double);
                }
                else if (type == typeof(string))
                {
                    toPersist[name] = new EntityProperty((string)value);
                }
                else
                {
                    using (var sw = new StringWriter())
                    {
                        try
                        {
                            jsonSerializerWithNonAbstractDefaultContractResolver.Serialize(sw, value, type);
                        }
                        catch (Exception)
                        {
                            throw new NotSupportedException($"The property type '{type.Name}' is not supported in Windows Azure Table Storage and it cannot be serialized with JSON.NET.");
                        }
                        toPersist[name] = new EntityProperty(sw.ToString());
                    }
                }
            }
            return toPersist;
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

        static bool TrySetNullable(EntityProperty value, object toCreate, PropertyInfo propertyInfo)
        {
            return
                TrySetNullable<bool>(value, toCreate, propertyInfo) ||
                TrySetNullable<DateTime>(value, toCreate, propertyInfo) ||
                TrySetNullable<Guid>(value, toCreate, propertyInfo) ||
                TrySetNullable<int>(value, toCreate, propertyInfo) ||
                TrySetNullable<double>(value, toCreate, propertyInfo) ||
                TrySetNullable<long>(value, toCreate, propertyInfo);
        }

        static bool TrySetNullable<TPrimitive>(EntityProperty property, object entity, PropertyInfo propertyInfo)
            where TPrimitive : struct
        {
            if (propertyInfo.PropertyType == typeof(TPrimitive))
            {
                var value = (TPrimitive?)property.PropertyAsObject;
                var nonNullableValue = value ?? default(TPrimitive);
                propertyInfo.SetValue(entity, nonNullableValue);
                return true;
            }

            if (propertyInfo.PropertyType == typeof(TPrimitive?))
            {
                var value = (TPrimitive?)property.PropertyAsObject;
                propertyInfo.SetValue(entity, value);
                return true;
            }

            return false;
        }

        public static TableQuery<DictionaryTableEntity> BuildWherePropertyQuery(Type type, string property, object value)
        {
            TableQuery<DictionaryTableEntity> query;

            var propertyInfo = type.GetProperty(property);
            if (propertyInfo == null)
            {
                return null;
            }

            if (propertyInfo.PropertyType == typeof(byte[]))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForBinary(property, QueryComparisons.Equal, (byte[])value));
            }
            else if (propertyInfo.PropertyType == typeof(bool))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForBool(property, QueryComparisons.Equal, (bool)value));
            }
            else if (propertyInfo.PropertyType == typeof(DateTime))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForDate(property, QueryComparisons.Equal, (DateTime)value));
            }
            else if (propertyInfo.PropertyType == typeof(Guid))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForGuid(property, QueryComparisons.Equal, (Guid)value));
            }
            else if (propertyInfo.PropertyType == typeof(int))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForInt(property, QueryComparisons.Equal, (int)value));
            }
            else if (propertyInfo.PropertyType == typeof(long))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForLong(property, QueryComparisons.Equal, (long)value));
            }
            else if (propertyInfo.PropertyType == typeof(double))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForDouble(property, QueryComparisons.Equal, (double)value));
            }
            else if (propertyInfo.PropertyType == typeof(string))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterCondition(property, QueryComparisons.Equal, (string)value));
            }
            else
            {
                throw new NotSupportedException($"The property type '{propertyInfo.PropertyType.Name}' is not supported in windows azure table storage");
            }

            return query;
        }

        static JsonSerializer jsonSerializer = JsonSerializer.Create();
        static JsonSerializer jsonSerializerWithNonAbstractDefaultContractResolver = new JsonSerializer
        {
            ContractResolver = new NonAbstractDefaultContractResolver(),
        };

        public static readonly DateTime StorageTableMinDateTime = new DateTime(1601, 1, 1);

        class NonAbstractDefaultContractResolver : DefaultContractResolver
        {
            protected override JsonObjectContract CreateObjectContract(Type objectType)
            {
                if (objectType.IsAbstract || objectType.IsInterface)
                {
                    throw new ArgumentException("Cannot serialize an abstract class/interface", nameof(objectType));
                }
                return base.CreateObjectContract(objectType);
            }
        }
    }
}