namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using Azure;

    /// <summary>
    /// Exception that is thrown when the table batch failed without throwing a storage exception. The exception gives access to the
    /// <see cref="Response"/> that exposes more details about the reason of failure.
    /// </summary>
    public sealed class AzureTableBatchOperationException : Exception
    {
        /// <summary>
        /// Initializes a new AzureTableBatchOperationException with a <see cref="Azure.Response"/>.
        /// </summary>
        public AzureTableBatchOperationException(Response result)
        {
            Result = result;
        }

        /// <summary>
        /// The <see cref="Response"/> exposing details about the reason of failure.
        /// </summary>
        public Response Result { get; }
    }
}