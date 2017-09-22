namespace Testing
{
    using System;

    public static class Utillities
    {
        public static string GetEnvConfiguredConnectionString()
        {
            var environmentVartiableName = "AzureStorageQueueTransport_ConnectionString";
            var connectionString = GetEnvironmentVariable(environmentVartiableName);
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception($"Oh no! We couldn't find an environment variable '{environmentVartiableName}' with Azure Storage connection string.");
            }

            return UnescapeLinuxEnvironmentVariableValue(connectionString);

            string GetEnvironmentVariable(string variable)
            {
                var candidate = Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.User);
                return string.IsNullOrWhiteSpace(candidate) ? Environment.GetEnvironmentVariable(variable) : candidate;
            }

            string UnescapeLinuxEnvironmentVariableValue(string originalValue)
            {
                return originalValue.TrimStart('\'').TrimEnd('\'');
            }
        }
    }
}