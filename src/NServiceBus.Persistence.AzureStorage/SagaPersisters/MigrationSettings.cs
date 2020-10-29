namespace NServiceBus
{
    using Settings;
    using Configuration.AdvancedExtensibility;
    using Persistence.AzureStorage.Config;

    /// <summary>
    /// Custom settings related to the outbox feature.
    /// </summary>
    public class MigrationSettings : ExposeSettings
    {
        internal MigrationSettings(SettingsHolder settings) : base(settings)
        {
        }

        /// <summary>
        /// By default the persistence operates in a backward compatible mode that tries to find sagas by using the secondary index property.
        /// Once all sagas have been migrated the lookup can be disabled.
        /// </summary>
        public void DisableSecondaryKeyLookupForSagasCorrelatedByProperties()
        {
            this.GetSettings().Set(WellKnownConfigurationKeys.MigrationMode, false);
        }

        /// <summary>
        /// Opt-in to full table scanning for sagas that have been stored with version 1.4 or earlier.
        /// </summary>
        /// <remarks>Enabling this also enables the migration mode meaning enabling this is mutually exclusive to <see cref="DisableSecondaryKeyLookupForSagasCorrelatedByProperties"/></remarks>
        public void AllowSecondaryKeyLookupToFallbackToFullTableScan()
        {
            this.GetSettings().Set(WellKnownConfigurationKeys.MigrationMode, true);
            this.GetSettings().Set(WellKnownConfigurationKeys.SagaStorageAssumeSecondaryIndicesExist, false);
        }
    }
}