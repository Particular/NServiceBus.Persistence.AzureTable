namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// Exception that is thrown when the table batch failed without throwing a storage exception. The exception gives access to the
    /// <see cref="TableResult"/> that exposes more details about the reason of failure.
    /// </summary>
    public sealed class TableBatchOperationException : Exception
    {
        /// <summary>
        /// Initializes a new TableBatchOperationException with a <see cref="TableResult"/>.
        /// </summary>
        public TableBatchOperationException(TableResult result)
        {
            Result = result;
        }

        /// <summary>
        /// The <see cref="TableResult"/> exposing details about the reason of failure.
        /// </summary>
        public TableResult Result { get; }
    }
}