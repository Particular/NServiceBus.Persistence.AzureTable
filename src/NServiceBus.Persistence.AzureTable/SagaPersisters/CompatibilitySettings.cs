namespace NServiceBus
{
    using Settings;
    using Configuration.AdvancedExtensibility;
    using Persistence.AzureTable;
    using System;

    /// <summary>
    /// Custom settings related to backward compatibility.
    /// </summary>
    public class CompatibilitySettings : ExposeSettings
    {
        internal CompatibilitySettings(SettingsHolder settings) : base(settings)
        {
        }

        /// <summary>
        /// By default the persistence operates in a backward compatible mode that tries to find sagas by using the secondary index property.
        /// Once all sagas have been migrated from version 2.4.x of the persister to the current version the lookup can be disabled.
        /// All migrated sagas do not contain a row called NServiceBus_2ndIndexKey
        /// </summary>
        public void DisableSecondaryKeyLookupForSagasCorrelatedByProperties()
        {
            var settings = this.GetSettings();
            if (settings.HasExplicitValue(WellKnownConfigurationKeys.SagaStorageAssumeSecondaryIndicesExist) ||
                settings.HasExplicitValue(WellKnownConfigurationKeys
                    .SagaStorageAssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey))
            {
                throw new InvalidOperationException(
                    "Compatibility mode cannot be disabled when `AllowSecondaryKeyLookupToFallbackToFullTableScan` or `AssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey` are called.");
            }

            settings.Set(WellKnownConfigurationKeys.SagaStorageCompatibilityMode, false);
        }

        /// <summary>
        /// Opt-in to full table scanning for sagas that have been stored with version 1.4 or earlier.
        /// </summary>
        /// <remarks>Enabling this also enables the migration mode meaning enabling this is mutually exclusive to <see cref="DisableSecondaryKeyLookupForSagasCorrelatedByProperties"/></remarks>
        public void AllowSecondaryKeyLookupToFallbackToFullTableScan()
        {
            var settings = this.GetSettings();
            if (settings.HasExplicitValue(WellKnownConfigurationKeys.SagaStorageCompatibilityMode) &&
                settings.Get<bool>(WellKnownConfigurationKeys.SagaStorageCompatibilityMode) == false)
            {
                throw new InvalidOperationException(
                    "Compatibility mode was disabled. `AllowSecondaryKeyLookupToFallbackToFullTableScan` requires compatibility mode to be enabled.");
            }

            settings.Set(WellKnownConfigurationKeys.SagaStorageAssumeSecondaryIndicesExist, false);
        }

        /// <summary>
        /// Sagas that have been stored with a secondary index used an empty RowKey on the secondary index entry. This prevents migrating to cosmos table API. During migration to cosmos table API it is advised to
        /// ensure all secondary index entries use RowKey = PartitionKey. By enabling this setting the secondary key lookups will assume RowKey = PartitionKey.
        /// </summary>
        /// <remarks>Enabling this also enables the migration mode meaning enabling this is mutually exclusive to <see cref="DisableSecondaryKeyLookupForSagasCorrelatedByProperties"/></remarks>
        public void AssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey()
        {
            var settings = this.GetSettings();
            if (settings.HasExplicitValue(WellKnownConfigurationKeys.SagaStorageCompatibilityMode) &&
                settings.Get<bool>(WellKnownConfigurationKeys.SagaStorageCompatibilityMode) == false)
            {
                throw new InvalidOperationException(
                    "Compatibility mode was disabled. `AssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey` requires compatibility mode to be enabled.");
            }

            settings.Set(WellKnownConfigurationKeys.SagaStorageAssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey, true);
        }
    }
}