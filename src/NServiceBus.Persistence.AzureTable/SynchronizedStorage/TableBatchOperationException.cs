namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using Azure;
    using Azure.Data.Tables.Models;

    /// <summary>
    /// Exception that is thrown when the table batch failed without throwing a storage exception. The exception gives access to the
    /// <see cref="TableTransactionResult"/> that exposes more details about the reason of failure.
    /// </summary>
    public sealed class TableBatchOperationException : Exception
    {
        /// <summary>
        /// Initializes a new TableBatchOperationException with a <see cref="TableTransactionResult"/>.
        /// </summary>
        public TableBatchOperationException(Response result) => Result = result;

        /// <summary>
        /// The <see cref="TableTransactionResult"/> exposing details about the reason of failure.
        /// </summary>
        public Response Result { get; }
    }
}