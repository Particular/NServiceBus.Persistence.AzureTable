namespace NServiceBus.Persistence.AzureTable
{
    static class AzureStorageSagaDefaults
    {
        public const bool CreateSchema = true;
        public const bool AssumeSecondaryIndicesExist = true;
        public const bool AssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey = false;
        public const bool CompatibilityModeEnabled = true;
    }
}