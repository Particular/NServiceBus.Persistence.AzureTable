namespace NServiceBus
{
    using System;
    using Configuration.AdvancedExtensibility;
    using Persistence.AzureTable;
    using Settings;

    /// <summary>
    /// Custom settings related to backward compatibility.
    /// </summary>
    public partial class CompatibilitySettings : ExposeSettings
    {
        internal CompatibilitySettings(SettingsHolder settings) : base(settings)
        {
        }

        /// <summary>
        /// Enables the persistence to operate in a backward compatible mode that tries to find sagas by using the secondary index property.
        /// Once all sagas have been migrated from version 2.4.x of the persister to the current version the lookup can be disabled no longer calling this method.
        /// All migrated sagas do not contain a row called NServiceBus_2ndIndexKey
        /// </summary>
        [ObsoleteEx(Message = "Compatibility mode is deprecated.", RemoveInVersion = "8.0", TreatAsErrorFromVersion = "7.0")]
        public void EnableSecondaryKeyLookupForSagasCorrelatedByProperties()
        {
            var settings = this.GetSettings();
            settings.Set(WellKnownConfigurationKeys.SagaStorageCompatibilityMode, true);
        }

        /// <summary>
        /// Opt-in to full table scanning for sagas that have been stored with version 1.4 or earlier.
        /// </summary>
        /// <remarks>Enabling this also requires enabling the compatibility mode by calling <see cref="EnableSecondaryKeyLookupForSagasCorrelatedByProperties"/></remarks>
        [ObsoleteEx(Message = "Compatibility mode is deprecated.", RemoveInVersion = "8.0", TreatAsErrorFromVersion = "7.0")]
        public void AllowSecondaryKeyLookupToFallbackToFullTableScan()
        {
            var settings = this.GetSettings();
            if (settings.Get<bool>(WellKnownConfigurationKeys.SagaStorageCompatibilityMode) == false)
            {
                throw new InvalidOperationException(
                    $"Compatibility mode was disabled. `{nameof(AllowSecondaryKeyLookupToFallbackToFullTableScan)}` requires the compatibility mode to be enabled by calling `{nameof(EnableSecondaryKeyLookupForSagasCorrelatedByProperties)}`.");
            }

            settings.Set(WellKnownConfigurationKeys.SagaStorageAssumeSecondaryIndicesExist, false);
        }

        /// <summary>
        /// Sagas that have been stored with a secondary index used an empty RowKey on the secondary index entry. This prevents migrating to cosmos table API. During migration to cosmos table API it is advised to
        /// ensure all secondary index entries use RowKey = PartitionKey. By enabling this setting the secondary key lookups will assume RowKey = PartitionKey.
        /// </summary>
        /// <remarks>Enabling this also requires enabling the compatibility mode by calling <see cref="EnableSecondaryKeyLookupForSagasCorrelatedByProperties"/></remarks>
        [ObsoleteEx(Message = "Compatibility mode is deprecated.", RemoveInVersion = "8.0", TreatAsErrorFromVersion = "7.0")]
        public void AssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey()
        {
            var settings = this.GetSettings();
            if (settings.Get<bool>(WellKnownConfigurationKeys.SagaStorageCompatibilityMode) == false)
            {
                throw new InvalidOperationException(
                    $"Compatibility mode was disabled. `{nameof(AssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey)}` requires the compatibility mode to be enabled by calling `{nameof(EnableSecondaryKeyLookupForSagasCorrelatedByProperties)}`.");
            }

            settings.Set(WellKnownConfigurationKeys.SagaStorageAssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey, true);
        }
    }
}
