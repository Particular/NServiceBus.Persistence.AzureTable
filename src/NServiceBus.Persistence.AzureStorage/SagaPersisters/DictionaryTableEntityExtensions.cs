namespace NServiceBus.Persistence.AzureStorage
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Microsoft.WindowsAzure.Storage.Table;

    static class DictionaryTableEntityExtensions
    {
        public static TEntity ToEntity<TEntity>(DictionaryTableEntity entity)
        {
            return (TEntity) ToEntity(typeof(TEntity), entity);
        }

        public static object ToEntity(Type entityType, DictionaryTableEntity entity)
        {
            if (entity == null)
            {
                return null;
            }

            var toCreate = Activator.CreateInstance(entityType);
            foreach (var propertyInfo in entityType.GetProperties())
            {
                if (entity.ContainsKey(propertyInfo.Name))
                {
                    if (propertyInfo.PropertyType == typeof(byte[]))
                    {
                        propertyInfo.SetValue(toCreate, entity[propertyInfo.Name].BinaryValue, null);
                    }
                    else if (propertyInfo.PropertyType == typeof(bool))
                    {
                        var boolean = entity[propertyInfo.Name].BooleanValue;
                        propertyInfo.SetValue(toCreate, boolean.HasValue && boolean.Value, null);
                    }
                    else if (propertyInfo.PropertyType == typeof(DateTime))
                    {
                        var dateTimeOffset = entity[propertyInfo.Name].DateTimeOffsetValue;
                        propertyInfo.SetValue(toCreate, dateTimeOffset?.DateTime ?? default(DateTime), null);
                    }
                    else if (propertyInfo.PropertyType == typeof(Guid))
                    {
                        var guid = entity[propertyInfo.Name].GuidValue;
                        propertyInfo.SetValue(toCreate, guid ?? default(Guid), null);
                    }
                    else if (propertyInfo.PropertyType == typeof(int))
                    {
                        var int32 = entity[propertyInfo.Name].Int32Value;
                        propertyInfo.SetValue(toCreate, int32 ?? default(int), null);
                    }
                    else if (propertyInfo.PropertyType == typeof(double))
                    {
                        var d = entity[propertyInfo.Name].DoubleValue;
                        propertyInfo.SetValue(toCreate, d ?? default(long), null);
                    }
                    else if (propertyInfo.PropertyType == typeof(long))
                    {
                        var int64 = entity[propertyInfo.Name].Int64Value;
                        propertyInfo.SetValue(toCreate, int64 ?? default(long), null);
                    }
                    else if (propertyInfo.PropertyType == typeof(string))
                    {
                        propertyInfo.SetValue(toCreate, entity[propertyInfo.Name].StringValue, null);
                    }
                    else
                    {
                        throw new NotSupportedException($"The property type '{propertyInfo.PropertyType.Name}' is not supported in windows azure table storage");
                    }
                }
            }
            return toCreate;
        }

        public static DictionaryTableEntity ToDictionaryTableEntity(object entity, DictionaryTableEntity toPersist, IEnumerable<PropertyInfo> properties)
        {
            foreach (var propertyInfo in properties)
            {
                if (propertyInfo.PropertyType == typeof(byte[]))
                {
                    toPersist[propertyInfo.Name] = new EntityProperty((byte[]) propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof(bool))
                {
                    toPersist[propertyInfo.Name] = new EntityProperty((bool) propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof(DateTime))
                {
                    toPersist[propertyInfo.Name] = new EntityProperty((DateTime) propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof(Guid))
                {
                    toPersist[propertyInfo.Name] = new EntityProperty((Guid) propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof(int))
                {
                    toPersist[propertyInfo.Name] = new EntityProperty((int) propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof(long))
                {
                    toPersist[propertyInfo.Name] = new EntityProperty((long) propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof(double))
                {
                    toPersist[propertyInfo.Name] = new EntityProperty((double) propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof(string))
                {
                    toPersist[propertyInfo.Name] = new EntityProperty((string) propertyInfo.GetValue(entity, null));
                }
                else
                {
                    throw new NotSupportedException($"The property type '{propertyInfo.PropertyType.Name}' is not supported in windows azure table storage");
                }
            }
            return toPersist;
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
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForBinary(property, QueryComparisons.Equal, (byte[]) value));
            }
            else if (propertyInfo.PropertyType == typeof(bool))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForBool(property, QueryComparisons.Equal, (bool) value));
            }
            else if (propertyInfo.PropertyType == typeof(DateTime))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForDate(property, QueryComparisons.Equal, (DateTime) value));
            }
            else if (propertyInfo.PropertyType == typeof(Guid))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForGuid(property, QueryComparisons.Equal, (Guid) value));
            }
            else if (propertyInfo.PropertyType == typeof(int))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForInt(property, QueryComparisons.Equal, (int) value));
            }
            else if (propertyInfo.PropertyType == typeof(long))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForLong(property, QueryComparisons.Equal, (long) value));
            }
            else if (propertyInfo.PropertyType == typeof(double))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForDouble(property, QueryComparisons.Equal, (double) value));
            }
            else if (propertyInfo.PropertyType == typeof(string))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterCondition(property, QueryComparisons.Equal, (string) value));
            }
            else
            {
                throw new NotSupportedException($"The property type '{propertyInfo.PropertyType.Name}' is not supported in windows azure table storage");
            }

            return query;
        }
    }
}