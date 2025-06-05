#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace NServiceBus;

using System;

public partial class CompatibilitySettings
{

    [ObsoleteEx(Message = "Compatibility mode is deprecated.", RemoveInVersion = "8.0",
        TreatAsErrorFromVersion = "7.0")]
    public void EnableSecondaryKeyLookupForSagasCorrelatedByProperties() => throw new NotImplementedException();

    [ObsoleteEx(Message = "Compatibility mode is deprecated.", RemoveInVersion = "8.0",
        TreatAsErrorFromVersion = "7.0")]
    public void AllowSecondaryKeyLookupToFallbackToFullTableScan() => throw new NotImplementedException();

    [ObsoleteEx(Message = "Compatibility mode is deprecated.", RemoveInVersion = "8.0",
        TreatAsErrorFromVersion = "7.0")]
    public void AssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey() => throw new NotImplementedException();
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member