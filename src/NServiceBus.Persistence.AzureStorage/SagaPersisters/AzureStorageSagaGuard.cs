namespace NServiceBus
{
    using System;

    static class AzureStorageSagaGuard
    {
        public static void CheckConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("ConnectionString should not be an empty string.", nameof(connectionString));
            }
        }
    }
}