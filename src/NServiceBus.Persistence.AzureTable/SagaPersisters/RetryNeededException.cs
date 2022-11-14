namespace NServiceBus.Persistence.AzureTable
{
    using System;

    /// <summary></summary>
    public class RetryNeededException : Exception
    {
        const string ErrorMessage = "This operation requires a retry as it wasn't possible to successfully process it now.";

        /// <summary></summary>
        public RetryNeededException() : base(ErrorMessage)
        {
        }

        /// <summary></summary>
        public RetryNeededException(Exception innerException) : base(ErrorMessage, innerException)
        {
        }
    }
}