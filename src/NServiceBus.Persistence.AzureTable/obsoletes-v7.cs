#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace NServiceBus;

using System;
using Configuration.AdvancedExtensibility;
using Particular.Obsoletes;
using Settings;

public partial class ConfigureAzureSagaStorage
{
    [ObsoleteMetadata(Message = "Compatibility mode is deprecated", RemoveInVersion = "8.0", TreatAsErrorFromVersion = "7.0")]
    [Obsolete("Compatibility mode is deprecated. Will be removed in version 8.0.0.", true)]
    public static CompatibilitySettings Compatibility(this PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> config) => throw new NotImplementedException();
}

[ObsoleteMetadata(Message = "Compatibility mode is deprecated", RemoveInVersion = "8.0", TreatAsErrorFromVersion = "7.0")]
[Obsolete("Compatibility mode is deprecated. Will be removed in version 8.0.0.", true)]
public class CompatibilitySettings : ExposeSettings
{
    [ObsoleteMetadata(Message = "Compatibility mode is deprecated", RemoveInVersion = "8.0", TreatAsErrorFromVersion = "7.0")]
    [Obsolete("Compatibility mode is deprecated. Will be removed in version 8.0.0.", true)]
    internal CompatibilitySettings(SettingsHolder settings) : base(settings) => throw new NotImplementedException();

    [ObsoleteMetadata(Message = "Compatibility mode is deprecated", RemoveInVersion = "8.0", TreatAsErrorFromVersion = "7.0")]
    [Obsolete("Compatibility mode is deprecated. Will be removed in version 8.0.0.", true)]
    public void EnableSecondaryKeyLookupForSagasCorrelatedByProperties() => throw new NotImplementedException();

    [ObsoleteMetadata(Message = "Compatibility mode is deprecated", RemoveInVersion = "8.0", TreatAsErrorFromVersion = "7.0")]
    [Obsolete("Compatibility mode is deprecated. Will be removed in version 8.0.0.", true)]
    public void AllowSecondaryKeyLookupToFallbackToFullTableScan() => throw new NotImplementedException();

    [ObsoleteMetadata(Message = "Compatibility mode is deprecated", RemoveInVersion = "8.0", TreatAsErrorFromVersion = "7.0")]
    [Obsolete("Compatibility mode is deprecated. Will be removed in version 8.0.0.", true)]
    public void AssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey() => throw new NotImplementedException();
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member