namespace NServiceBus
{
    class AzureTimeoutStorageDefaults
    {
        /// <summary>
        /// Azure Storage table name for Timeout Manager Data. Default is 'TimeoutManagerDataTable'.
        /// </summary>
        public const string TimeoutManagerDataTableName = "TimeoutManagerDataTable";

        /// <summary>
        /// Azure Storage table name for Timeout Data. Default is 'TimeoutDataTableName'.
        /// </summary>
        public const string TimeoutDataTableName = "TimeoutDataTableName";

        /// <summary>
        /// Catchup interval in seconds. Default is 1 hour (3600 seconds).
        /// </summary>
        public const int CatchUpInterval = 3600;

        /// <summary>
        /// DateTime format used for creating the Partition Key Scope value. Default value is 'yyyyMMddHH'.
        /// </summary>
        public const string PartitionKeyScope = "yyyyMMddHH";

        /// <summary>
        /// Flag to indicate that an Azure Storage schema should be created if it is not found. Defaults to true.
        /// </summary>
        public const bool CreateSchema = true;

        /// <summary>
        /// Azure Storage blob name for Timeout State. Default is 'timeoutstate'.
        /// </summary>
        public const string TimeoutStateContainerName = "timeoutstate";
    }
}