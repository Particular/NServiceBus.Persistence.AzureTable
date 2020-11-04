namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Linq;
    using Microsoft.Azure.Cosmos.Table;

    public static class ConnectionStringHelper
    {
        public static string GetEnvConfiguredConnectionStringByCallerConvention(this object caller)
        {
            var tableApiType = caller.GetType().Assembly.GetName().Name.Split(new[] {"."}, StringSplitOptions.RemoveEmptyEntries)
                .Reverse().Skip(1).Take(1).SingleOrDefault();

            return string.IsNullOrEmpty(tableApiType) ?
                GetEnvConfiguredConnectionStringForPersistence() :
                GetEnvConfiguredConnectionStringForPersistence(tableApiType);
        }

        public static string GetEnvConfiguredConnectionStringForPersistence(string tableApiType = "StorageTable")
        {
            var environmentVartiableName = $"AzureTable_{tableApiType}_ConnectionString";
            Console.WriteLine($":: Using connection string found in the '{environmentVartiableName}' environment variable. ::");
            var connectionString = GetEnvironmentVariable(environmentVartiableName);
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception($"Oh no! We couldn't find an environment variable '{environmentVartiableName}' with Azure Storage connection string.");
            }

            return connectionString;
        }

        static string GetEnvironmentVariable(string variable)
        {
            var candidate = Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.User);
            return string.IsNullOrWhiteSpace(candidate) ? Environment.GetEnvironmentVariable(variable) : candidate;
        }

        // the SDK uses exactly this method of changing the underlying executor
        public static bool IsPremiumEndpoint(CloudTableClient cloudTableClient)
        {
            var lowerInvariant = cloudTableClient.StorageUri.PrimaryUri.OriginalString.ToLowerInvariant();
            return lowerInvariant.Contains("https://localhost") && cloudTableClient.StorageUri.PrimaryUri.Port != 10002 || lowerInvariant.Contains(".table.cosmosdb.") || lowerInvariant.Contains(".table.cosmos.");
        }
    }
}