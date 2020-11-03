namespace NServiceBus
{
    using Persistence.AzureStorage;

    /// <summary>
    /// Represents the table name when the table information is provided at runtime through the pipeline.
    /// </summary>
    public readonly struct TableInformation
    {
        /// <summary>
        /// Initializes the container information with a specified table name.
        /// </summary>
        /// <param name="tableName">The name of the table to use.</param>
        public TableInformation(string tableName)
        {
            Guard.AgainstNullAndEmpty(nameof(tableName), tableName);

            TableName = tableName;
        }

        /// <summary>
        /// The name of the table to be used.
        /// </summary>
        public string TableName { get; }
    }
}