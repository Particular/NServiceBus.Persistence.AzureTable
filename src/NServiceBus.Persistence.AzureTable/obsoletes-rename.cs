#pragma warning disable 1591

namespace NServiceBus
{
    using Persistence;

    [ObsoleteEx(Message = "The persistence has been renamed from AzureStorage to AzureTable.",
        ReplacementTypeOrMember = nameof(AzureTablePersistence),
        TreatAsErrorFromVersion = "3",
        RemoveInVersion = "4")]
    public class AzureStoragePersistence : PersistenceDefinition
    {
    }

    [ObsoleteEx(Message = "The saga persistence feature is no longer exposed as a public type.",
        TreatAsErrorFromVersion = "3",
        RemoveInVersion = "4")]
    public class AzureStorageSagaPersistence
    {
    }

    [ObsoleteEx(Message = "The subscription persistence feature is no longer exposed as a public type.",
        TreatAsErrorFromVersion = "3",
        RemoveInVersion = "4")]
    public class AzureStorageSubscriptionPersistence
    {
    }
}
#pragma warning restore 1591