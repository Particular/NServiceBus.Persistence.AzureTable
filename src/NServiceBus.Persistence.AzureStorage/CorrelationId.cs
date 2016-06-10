namespace NServiceBus
{
    using System;
    using System.Threading;
    using Microsoft.WindowsAzure.Storage;

    /// <summary>
    /// Provides a builder for a client request id, correlation all the values by the root created per instance of it.
    /// </summary>
    class CorrelationId
    {
        public CorrelationId()
        {
            RootId = Guid.NewGuid().ToString();
        }

        public string Next()
        {
            var value = Interlocked.Increment(ref counter);
            return RootId + "-" + value;
        }

        /// <summary>
        /// Provides an <see cref="OperationContext" /> with <see cref="OperationContext.ClientRequestID" /> set to
        /// <see cref="Next" /> value.
        /// </summary>
        public OperationContext NextContext()
        {
            return new OperationContext
            {
                ClientRequestID = Next()
            };
        }

        /// <summary>
        /// Extracts the root request id from the passed <paramref name="value" />.
        /// </summary>
        public static bool TryGetRootRequestId(string value, out Guid rootId)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                rootId = Guid.Empty;
                return false;
            }

            var raw = value.Substring(0, GuidLength);
            return Guid.TryParse(raw, out rootId);
        }

        readonly string RootId;
        int counter;

        static readonly int GuidLength = Guid.Empty.ToString().Length;
    }
}