namespace NServiceBus.Persistence.AzureStorage
{
    using System;
    using Sagas;
    using System.Security.Cryptography;
    using System.Text;
    using Newtonsoft.Json;

    class SagaIdGenerator : ISagaIdGenerator
    {
        public Guid Generate(SagaIdGeneratorContext context)
        {
            if (context.CorrelationProperty == SagaCorrelationProperty.None)
            {
                // TODO: verify!
                throw new Exception("The Azure Storage saga persister doesn't support custom saga finders.");
            }

            return Generate(context.SagaMetadata.SagaEntityType, context.CorrelationProperty.Name, context.CorrelationProperty.Value);
        }

        public static Guid Generate(Type sagaEntityType, string correlationPropertyName, object correlationPropertyValue) => Generate(sagaEntityType.FullName, correlationPropertyName, correlationPropertyValue);

        public static Guid Generate(string sagaEntityTypeFullName, string correlationPropertyName, object correlationPropertyValue)
        {
            // assumes single correlated sagas since v6 doesn't allow more than one corr prop
            // will still have to use a GUID since moving to a string id will have to wait since its a breaking change
            var serializedPropertyValue = JsonConvert.SerializeObject(correlationPropertyValue);
            return DeterministicGuid($"{sagaEntityTypeFullName}_{correlationPropertyName}_{serializedPropertyValue}");
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
    }
}