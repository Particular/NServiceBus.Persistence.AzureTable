namespace NServiceBus.Persistence.AzureStorage.SecondaryIndices
{
    using System;
    using System.IO;
    using Newtonsoft.Json;
    using Sagas;
    using System.Security.Cryptography;
    using System.Text;

    static class SecondaryIndexKeyBuilder
    {
        public static PartitionRowKeyTuple BuildTableKey(Type sagaType, SagaCorrelationProperty correlationProperty)
        {
            var sagaDataTypeName = sagaType.FullName;
            var partitionKey = $"Index_{sagaDataTypeName}_{correlationProperty.Name}_{Serialize(correlationProperty.Value)}";
            return new PartitionRowKeyTuple(partitionKey, DeterministicGuid(partitionKey).ToString());
        }

        static Guid DeterministicGuid(string src)
        {
            var stringBytes = Encoding.UTF8.GetBytes(src);

            using (var sha1CryptoServiceProvider = new SHA1CryptoServiceProvider())
            {
                var hashedBytes = sha1CryptoServiceProvider.ComputeHash(stringBytes);
                Array.Resize(ref hashedBytes, 16);
                return new Guid(hashedBytes);
            }
        }

        static string Serialize(object propertyValue)
        {
            using (var writer = new StringWriter())
            {
                jsonSerializer.Serialize(writer, propertyValue);
                writer.Flush();
                return writer.ToString();
            }
        }

        static JsonSerializer jsonSerializer = new JsonSerializer();
    }
}