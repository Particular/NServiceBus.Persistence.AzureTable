namespace NServiceBus.AzureStoragePersistence.SagaDeduplicator.Index
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public sealed class SagaJsonMapper
    {
        public const string ETag = "$ETag";
        public const string ChooseThisSaga = "$Choose_this_saga";
        static readonly HashSet<string> SpecialProperties = new HashSet<string> { ETag };
        readonly CloudTable cloudTable;

        public SagaJsonMapper(string connectionString, string sagaTypeName)
        {
            cloudTable = SagaIndexer.GetTable(connectionString, sagaTypeName);
        }

        public void Download(Guid id, TextWriter writer)
        {
            var entity = cloudTable.Execute(TableOperation.Retrieve<DynamicTableEntity>(id.ToString(), id.ToString())).Result as DynamicTableEntity;
            if (entity == null)
            {
                return;
            }

            using (var jsonWriter = new JsonTextWriter(writer))
            {
                jsonWriter.Formatting = Formatting.Indented;

                var properties = entity.Properties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.PropertyAsObject);
                properties.Add(ETag, entity.ETag);
                properties.Add(ChooseThisSaga, false);

                JObject.FromObject(properties).WriteTo(jsonWriter);

                jsonWriter.Flush();
            }
        }

        public UploadStatus TryUpload(Guid id, JsonReader reader)
        {
            var entity = cloudTable.Execute(TableOperation.Retrieve<DynamicTableEntity>(id.ToString(), id.ToString())).Result as DynamicTableEntity;
            if (entity == null)
            {
                return UploadStatus.NotFound;
            }

            var jo = JObject.Load(reader);
            var shouldUpload = (bool) jo.GetValue(ChooseThisSaga);
            jo.Remove(ChooseThisSaga);
            if (shouldUpload == false)
            {
                return UploadStatus.NotSelected;
            }


            var eTag = (string)jo.GetValue(ETag);
            if (entity.ETag != eTag)
            {
                return UploadStatus.DifferentETag;
            }

            entity.ETag = eTag;

            foreach (var p in jo.Properties().Where(jp => SpecialProperties.Contains(jp.Name) == false))
            {
                var value = ((JValue)p.Value).Value;
                entity[p.Name] = BuildNewProperty(entity, p, value);
            }

            var status = (HttpStatusCode)cloudTable.Execute(TableOperation.Replace(entity)).HttpStatusCode;
            if (status == HttpStatusCode.OK || status == HttpStatusCode.NoContent)
            {
                return UploadStatus.Selected;
            }
            if (status == HttpStatusCode.NotFound)
            {
                return UploadStatus.NotFound;
            }

            return UploadStatus.DifferentETag;
        }

        private static EntityProperty BuildNewProperty(DynamicTableEntity entity, JProperty jsonProperty, object value)
        {
            EntityProperty entityProperty;
            if (entity.Properties.TryGetValue(jsonProperty.Name, out entityProperty))
            {
                // this is a special case as Json does not recognize ints
                if (entityProperty.PropertyType == EdmType.Int32)
                {
                    return new EntityProperty((int) (long) value);
                }
            }
            return EntityProperty.CreateEntityPropertyFromObject(value);
        }

        public enum UploadStatus
        {
            Selected = 0,
            NotFound = 1,
            DifferentETag = 2,
            NotSelected
        }
    }
}