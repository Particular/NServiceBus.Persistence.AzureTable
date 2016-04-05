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

        private static ulong TransformLong(object o)
        {
            return (ulong)(long)o;
        }

        private static ulong TransformInt(object o)
        {
            return (ulong)(int)o;
        }

        private static unsafe ulong TransformGuid(object o)
        {
            var g = (Guid)o;
            return *(ulong*)&g;
        }

        private static ulong TransformDouble(object o)
        {
            var dt = (double)o;
            return (ulong)dt;
        }

        private static unsafe ulong TransformDateTime(object o)
        {
            var dt = (DateTime)o;
            return *(ulong*)&dt;
        }

        private static ulong TransformBytes(object o)
        {
            var bytes = (byte[])o;
            return Murmur3.Hash(bytes);
        }

        private static ulong TransformString(object o)
        {
            var str = (string)o;
            return Murmur3.Hash(str);
        }
    }
}