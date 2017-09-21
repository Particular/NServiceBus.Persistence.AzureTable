#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace NServiceBus
{
    using System;
    using Persistence;

    public static partial class ConfigureAzureSagaStorage
    {
        [ObsoleteEx(Message = "Replaced with new API.", TreatAsErrorFromVersion = "2.0.0", RemoveInVersion = "3.0.0", ReplacementTypeOrMember = ".AssumeSecondaryIndicesExist(bool)")]
        public static PersistenceExtensions<AzureStoragePersistence, StorageType.Sagas> AssumeSecondaryIndicesExist(this PersistenceExtensions<AzureStoragePersistence, StorageType.Sagas> config) => throw new NotImplementedException();
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member