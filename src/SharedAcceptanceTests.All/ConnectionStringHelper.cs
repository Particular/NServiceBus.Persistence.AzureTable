namespace NServiceBus.Testing
{
    using System;
    using System.Linq;
    using Azure.Data.Tables;

    public static class ConnectionStringHelper
    {
        public static string GetEnvConfiguredConnectionStringByCallerConvention(this object caller)
        {
            // [Prefix.]{TableApiType}.ProjectType --> ProjectType (skipped) TableApiType (taken) [Prefix] --> TableApiType
            var tableApiType = caller.GetType().Assembly.GetName().Name.Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries)
                .Reverse().Skip(1).Take(1).SingleOrDefault();

            return GetEnvConfiguredConnectionStringForPersistence(tableApiType);
        }

        public static string GetEnvConfiguredConnectionStringForPersistence(string tableApiType)
        {
            if (string.IsNullOrEmpty(tableApiType))
            {
                throw new Exception("The table API type must either be `StorageTable` or `CosmosDB`.");
            }

            var environmentVariableName = $"AzureTable_{tableApiType}_ConnectionString";
            Console.WriteLine($":: Using connection string found in the '{environmentVariableName}' environment variable. ::");
            var connectionString = GetEnvironmentVariable(environmentVariableName);
            if (string.IsNullOrEmpty(connectionString))
            {
                return "UseDevelopmentStorage=true";
            }

            return connectionString;
        }

        static string GetEnvironmentVariable(string variable)
        {
            var candidate = Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.User);
            return string.IsNullOrWhiteSpace(candidate) ? Environment.GetEnvironmentVariable(variable) : candidate;
        }

        // the SDK uses exactly this method of changing the underlying executor
        public static bool IsPremiumEndpoint(TableServiceClient cloudTableClient)
        {
            // TODO: figure out how to verify whether it's a premium endpoint or not
            return true;
            // var lowerInvariant = cloudTableClient.StorageUri.PrimaryUri.OriginalString.ToLowerInvariant();
            // return (lowerInvariant.Contains("https://localhost") && cloudTableClient.StorageUri.PrimaryUri.Port != 10002) || lowerInvariant.Contains(".table.cosmosdb.") || lowerInvariant.Contains(".table.cosmos.");
        }
    }
}