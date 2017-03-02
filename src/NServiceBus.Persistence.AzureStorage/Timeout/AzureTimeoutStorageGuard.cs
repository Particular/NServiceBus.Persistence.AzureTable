namespace NServiceBus.Timeout
{
    using System;
    using System.Text.RegularExpressions;

    static class AzureTimeoutStorageGuard
    {
        public static void CheckConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("ConnectionString should not be an empty string.", nameof(connectionString));
            }
        }

        public static void CheckTableName(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException("Table name should not be an empty string.", nameof(tableName));
            }

            var tableNameRegex = new Regex("^[A-Za-z][A-Za-z0-9]{2,62}(?!tables)$");
            if (!tableNameRegex.IsMatch(tableName))
            {
                // error message is following MSFT guidelines http://msdn.microsoft.com/library/azure/dd179338.aspx
                const string errorMessage = "Invalid table name. Valid name should follow these rules:\n"
                                            + " Contain only alphanumeric characters.\n"
                                            + " Cannot begin with a numeric character.\n"
                                            + " Must be from 3 to 63 characters long."
                                            + " Avoid reserved names, such as \"tables\".";
                throw new ArgumentException(errorMessage, nameof(tableName));
            }
        }

        public static void CheckCatchUpInterval(int catchUpInterval)
        {
            if (catchUpInterval < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(catchUpInterval), catchUpInterval, "catchUpInterval should not be less than 1 second.");
            }
        }

        public static void CheckPartitionKeyScope(string partitionKeyScope)
        {
            if (string.IsNullOrWhiteSpace(partitionKeyScope))
            {
                throw new ArgumentException("Partition key scope should not be an empty string.", nameof(partitionKeyScope));
            }

            var partitionKeyRegex = new Regex(@"^[a-zA-Z0-9\-_]{4,}$");
            if (!partitionKeyRegex.IsMatch(partitionKeyScope))
            {
                var errorMessage =
                    @"Invalid partition key scope. Valid key should follow the following regular expression: ^[a-zA-Z0-9\-_]{4,}$ and comply with .NET DateTime formatting string rules. Examples are:
yyyy-MM-dd-mm
yyyyMMddHH (default)
yyyy_MM_dd";
                throw new ArgumentException(errorMessage, nameof(partitionKeyScope));
            }
        }
    }
}
