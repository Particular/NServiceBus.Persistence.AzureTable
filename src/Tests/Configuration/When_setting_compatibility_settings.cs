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
        public void Should_enable_compatibility_mode_when_enableSecondaryKeyLookupForSagasCorrelatedByProperties_called()
        {
            settings.EnableSecondaryKeyLookupForSagasCorrelatedByProperties();

            Assert.That(settings.GetSettings().Get<bool>(WellKnownConfigurationKeys.SagaStorageCompatibilityMode), Is.True);
        }

        [Test]
        public void Should_throw_when_compatibility_mode_off_and_allowSecondaryKeyLookupToFallbackToFullTableScan()
        {
            Assert.Throws<InvalidOperationException>(() => settings.AllowSecondaryKeyLookupToFallbackToFullTableScan());
        }

        [Test]
        public void Should_enable_when_compatibility_mode_on_and_allowSecondaryKeyLookupToFallbackToFullTableScan()
        {
            settings.EnableSecondaryKeyLookupForSagasCorrelatedByProperties();

            settings.AllowSecondaryKeyLookupToFallbackToFullTableScan();

            Assert.That(settings.GetSettings().Get<bool>(WellKnownConfigurationKeys.SagaStorageAssumeSecondaryIndicesExist), Is.False);
        }

        [Test]
        public void Should_throw_when_compatibility_mode_off_and_assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey()
        {
            Assert.Throws<InvalidOperationException>(() => settings.AssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey());
        }

        [Test]
        public void Should_enable_when_compatibility_mode_on_and_assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey()
        {
            settings.EnableSecondaryKeyLookupForSagasCorrelatedByProperties();

            settings.AssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey();

            Assert.That(settings.GetSettings().Get<bool>(WellKnownConfigurationKeys.SagaStorageAssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey), Is.True);
        }

        CompatibilitySettings settings;
    }
}