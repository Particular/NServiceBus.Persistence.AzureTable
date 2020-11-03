namespace NServiceBus.Persistence.AzureTable
{
    using System;

    /// <summary></summary>
    public class RetryNeededException : Exception
    {
        const string errorMessage = "This operation requires a retry as it wasn't possible to successfully process it now.";

        /// <summary></summary>
        public RetryNeededException() : base(errorMessage)
        {
        }

        /// <summary></summary>
        public RetryNeededException(Exception innerException) : base(errorMessage, innerException)
        {
        }
    }
}
