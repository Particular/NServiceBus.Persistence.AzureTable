namespace NServiceBus
{
    static class AzureStorageSagaDefaults
    {
        public const bool CreateSchema = true;
        public const bool AssumeSecondaryIndicesExist = false;
        public const bool MigrationModeEnabled = false;
    }
}