namespace NServiceBus
{
    using Persistence.AzureStorage;

    /// <summary>
    ///
    /// </summary>
    public readonly struct TableInformation
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="tableName">TODO</param>
        public TableInformation(string tableName)
        {
            Guard.AgainstNullAndEmpty(nameof(tableName), tableName);

            TableName = tableName;
        }

        /// <summary>
        ///
        /// </summary>
        public string TableName { get; }
    }
}