#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace NServiceBus;

using System;
using Configuration.AdvancedExtensibility;
using Settings;

public partial class ConfigureAzureSagaStorage
{
    [ObsoleteEx(Message = "Compatibility mode is deprecated.", RemoveInVersion = "8.0", TreatAsErrorFromVersion = "7.0")]
    public static CompatibilitySettings Compatibility(this PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> config) => throw new NotImplementedException();
}

[ObsoleteEx(Message = "Compatibility mode is deprecated.", RemoveInVersion = "8.0", TreatAsErrorFromVersion = "7.0")]
public class CompatibilitySettings : ExposeSettings
{
    [ObsoleteEx(Message = "Compatibility mode is deprecated.", RemoveInVersion = "8.0", TreatAsErrorFromVersion = "7.0")]
    internal CompatibilitySettings(SettingsHolder settings) : base(settings) => throw new NotImplementedException();

    [ObsoleteEx(Message = "Compatibility mode is deprecated.", RemoveInVersion = "8.0", TreatAsErrorFromVersion = "7.0")]
    public void EnableSecondaryKeyLookupForSagasCorrelatedByProperties() => throw new NotImplementedException();

    [ObsoleteEx(Message = "Compatibility mode is deprecated.", RemoveInVersion = "8.0", TreatAsErrorFromVersion = "7.0")]
    public void AllowSecondaryKeyLookupToFallbackToFullTableScan() => throw new NotImplementedException();

    [ObsoleteEx(Message = "Compatibility mode is deprecated.", RemoveInVersion = "8.0", TreatAsErrorFromVersion = "7.0")]
    public void AssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey() => throw new NotImplementedException();
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member