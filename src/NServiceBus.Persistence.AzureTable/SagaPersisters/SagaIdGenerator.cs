namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.Buffers;
#if NETFRAMEWORK
    using System.Runtime.InteropServices;
#endif
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
                throw new Exception("The Azure Table saga persister doesn't support custom saga finders.");
            }

            return Generate(context.SagaMetadata.SagaEntityType, context.CorrelationProperty);
        }

        public static Guid Generate<TSagaData>(SagaCorrelationProperty correlationProperty)
            where TSagaData : IContainSagaData
            => Generate(typeof(TSagaData), correlationProperty);

        public static Guid Generate(Type sagaEntityType, SagaCorrelationProperty correlationProperty) => Generate(sagaEntityType.FullName, correlationProperty);

        public static Guid Generate(string sagaEntityTypeFullName, SagaCorrelationProperty correlationProperty)
        {
            // assumes single correlated sagas since v6 doesn't allow more than one corr prop
            // will still have to use a GUID since moving to a string id will have to wait since its a breaking change
            var serializedPropertyValue = JsonConvert.SerializeObject(correlationProperty.Value);
            return DeterministicGuid($"{sagaEntityTypeFullName}_{correlationProperty.Name}_{serializedPropertyValue}");
        }

#if NETFRAMEWORK
        static Guid DeterministicGuid(string src)
        {
            var byteCount = Encoding.UTF8.GetByteCount(src);
            var buffer = ArrayPool<byte>.Shared.Rent(byteCount);
            try
            {
                var numberOfBytesWritten = Encoding.UTF8.GetBytes(src.AsSpan(), buffer);

                using var sha1CryptoServiceProvider = SHA1.Create();
                var guidBytes = sha1CryptoServiceProvider.ComputeHash(buffer, 0, numberOfBytesWritten).AsSpan().Slice(0, 16);
                if (!MemoryMarshal.TryRead<Guid>(guidBytes, out var deterministicGuid))
                {
                    deterministicGuid = new Guid(guidBytes.ToArray());
                }
                return deterministicGuid;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
            }
        }
#endif
#if NET
        static Guid DeterministicGuid(string src)
        {
            var byteCount = Encoding.UTF8.GetByteCount(src);
            var stringBuffer = ArrayPool<byte>.Shared.Rent(byteCount);
            try
            {
                var numberOfBytesWritten = Encoding.UTF8.GetBytes(src.AsSpan(), stringBuffer);

                using var sha1CryptoServiceProvider = SHA1.Create();
                Span<byte> hashBuffer = stackalloc byte[20];
                if (!sha1CryptoServiceProvider.TryComputeHash(stringBuffer.AsSpan().Slice(0, numberOfBytesWritten), hashBuffer, out _))
                {
                    var hashBufferLocal = sha1CryptoServiceProvider.ComputeHash(stringBuffer, 0, numberOfBytesWritten);
                    hashBufferLocal.CopyTo(hashBuffer);
                }

                var guidBytes = hashBuffer.Slice(0, 16);
                return new Guid(guidBytes);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(stringBuffer, clearArray: true);
            }
        }
#endif
    }

#if NETFRAMEWORK
    static class SpanExtensions
    {
        public static unsafe int GetBytes(this Encoding encoding, ReadOnlySpan<char> src, Span<byte> dst)
        {
            if (src.IsEmpty)
            {
                return 0;
            }

            fixed (char* chars = &src.GetPinnableReference())
            fixed (byte* bytes = &dst.GetPinnableReference())
            {
                return encoding.GetBytes(chars, src.Length, bytes, dst.Length);
            }
        }
    }
#endif
}
