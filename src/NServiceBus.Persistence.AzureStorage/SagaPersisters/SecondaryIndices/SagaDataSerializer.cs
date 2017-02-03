namespace NServiceBus.Persistence.AzureStorage.SecondaryIndices
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    class SagaDataSerializer
    {
        public static byte[] SerializeSagaData<TSagaData>(TSagaData sagaData) where TSagaData : IContainSagaData
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var zipped = new GZipStream(memoryStream, CompressionMode.Compress))
                using (var writer = new StreamWriter(zipped))
                {
                    serializer.Serialize(writer, sagaData);
                    writer.Flush();
                }

                return memoryStream.ToArray();
            }
        }

        public static IContainSagaData DeserializeSagaData(Type sagaType, byte[] value)
        {
            using (var memoryStream = new MemoryStream(value))
            using (var zipped = new GZipStream(memoryStream, CompressionMode.Decompress))
            using (var reader = new StreamReader(zipped))
            {
                return (IContainSagaData) serializer.Deserialize(reader, sagaType);
            }
        }

        static JsonSerializer serializer = new JsonSerializer
        {
            ContractResolver = new SagaOnlyPropertiesDataContractResolver()
        };

        class SagaOnlyPropertiesDataContractResolver : DefaultContractResolver
        {
            public SagaOnlyPropertiesDataContractResolver() : base(true) // for performance
            {
            }

            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                var properties = new HashSet<string>(AzureSagaPersister.SelectPropertiesToPersist(type).Select(pi => pi.Name));
                return base.CreateProperties(type, memberSerialization)
                    .Where(p => properties.Contains(p.PropertyName))
                    .ToArray();
            }
        }
    }
}