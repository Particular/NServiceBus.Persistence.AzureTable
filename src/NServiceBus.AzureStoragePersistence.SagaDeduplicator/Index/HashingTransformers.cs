namespace NServiceBus.AzureStoragePersistence.SagaDeduplicator.Index
{
    using System;
    using Microsoft.WindowsAzure.Storage.Table;

    public static class HashingTransformers
    {
        /// <summary>
        /// Gets a function providing a transformation from the specified <paramref name="type"/> to the hashed value represented as <see cref="ulong"/>.
        /// </summary>
        public static Func<object, ulong> GetHashingTransformer(EdmType type)
        {
            switch (type)
            {
                case EdmType.String:
                    return TransformString;
                case EdmType.Binary:
                    return TransformBytes;
                case EdmType.DateTime:
                    return TransformDateTime;
                case EdmType.Double:
                    return TransformDouble;
                case EdmType.Guid:
                    return TransformGuid;
                case EdmType.Int32:
                    return TransformInt;
                case EdmType.Int64:
                    return TransformLong;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        static ulong TransformLong(object o)
        {
            return (ulong)(long)o;
        }

        static ulong TransformInt(object o)
        {
            return (ulong)(int)o;
        }

        static unsafe ulong TransformGuid(object o)
        {
            var g = (Guid)o;
            return *(ulong*)&g;
        }

        static ulong TransformDouble(object o)
        {
            var dt = (double)o;
            return (ulong)dt;
        }

        static unsafe ulong TransformDateTime(object o)
        {
            var dt = (DateTime)o;
            return *(ulong*)&dt;
        }

        static ulong TransformBytes(object o)
        {
            var bytes = (byte[])o;
            return Murmur3.Hash(bytes);
        }

        static ulong TransformString(object o)
        {
            var str = (string)o;
            return Murmur3.Hash(str);
        }
    }
}