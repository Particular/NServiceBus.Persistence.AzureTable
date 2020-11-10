namespace NServiceBus.Persistence.AzureTable.Tests
{
    using System;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using NServiceBus.Persistence.AzureTable;
    using NServiceBus.Settings;
    using NUnit.Framework;

    [TestFixture]
    public class When_setting_compatibility_settings
    {
        [SetUp]
        public void Setup()
        {
            var settingsHolder = new SettingsHolder();
            settings = new CompatibilitySettings(settingsHolder);
            settings.GetSettings().SetDefault(WellKnownConfigurationKeys.SagaStorageAssumeSecondaryIndicesExist, AzureStorageSagaDefaults.AssumeSecondaryIndicesExist);
            settings.GetSettings().SetDefault(WellKnownConfigurationKeys.SagaStorageAssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey, AzureStorageSagaDefaults.AssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey);
            settings.GetSettings().SetDefault(WellKnownConfigurationKeys.SagaStorageCompatibilityMode, AzureStorageSagaDefaults.CompatibilityModeEnabled);
        }

        [Test]
        public void Should_throw_when_compatibility_mode_off_and_allowSecondaryKeyLookupToFallbackToFullTableScan()
        {
            settings.DisableSecondaryKeyLookupForSagasCorrelatedByProperties();
            Assert.Throws<InvalidOperationException>(() => settings.AllowSecondaryKeyLookupToFallbackToFullTableScan());
        }

        [Test]
        public void Should_throw_when_compatibility_mode_off_and_assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey()
        {
            settings.DisableSecondaryKeyLookupForSagasCorrelatedByProperties();
            Assert.Throws<InvalidOperationException>(() => settings.AssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey());
        }

        [Test]
        public void Should_throw_when_assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey_and_disable_compatibility_mode_called()
        {
            settings.AssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey();
            Assert.Throws<InvalidOperationException>(() => settings.DisableSecondaryKeyLookupForSagasCorrelatedByProperties());
        }

        [Test]
        public void Should_throw_when_allowSecondaryKeyLookupToFallbackToFullTableScan_and_disable_compatibility_mode_called()
        {
            settings.AllowSecondaryKeyLookupToFallbackToFullTableScan();
            Assert.Throws<InvalidOperationException>(() => settings.DisableSecondaryKeyLookupForSagasCorrelatedByProperties());
        }

        CompatibilitySettings settings;

    }
}