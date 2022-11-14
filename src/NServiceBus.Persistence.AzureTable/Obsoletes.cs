#pragma warning disable CS1591
namespace NServiceBus.Persistence.AzureTable
{
    [ObsoleteEx(Message = "The IProvideCloudTableClient is deprecated in favor of IProvideTableServiceClient, which supports the TableServiceClient API provided by Azure.Data.Tables.", TreatAsErrorFromVersion = "5.0.0", RemoveInVersion = "6.0.0")]
    public interface IProvideCloudTableClient
    {
        object Client { get; }
    }

    [ObsoleteEx(Message = "The IProvideCloudTableClientForSubscriptions is deprecated in favor of IProvideTableServiceClientForSubscriptions, which supports the TableServiceClient API provided by Azure.Data.Tables.", TreatAsErrorFromVersion = "5.0.0", RemoveInVersion = "6.0.0")]
    public interface IProvideCloudTableClientForSubscriptions
    {
        object Client { get; }
    }
}

namespace NServiceBus
{
    using System;

    public static partial class ConfigureAzureSagaStorage
    {
        [ObsoleteEx(Message = "The UseCloudTableClient method is deprecated in favor of the UseTableServiceClient method, which supports the TableServiceClient API provided by Azure.Data.Tables.", TreatAsErrorFromVersion = "5.0.0", RemoveInVersion = "6.0.0")]
        public static PersistenceExtensions<AzureTablePersistence> UseCloudTableClient(
            this PersistenceExtensions<AzureTablePersistence> config, object client) =>
            throw new NotImplementedException();
    }

    public static partial class ConfigureAzureSubscriptionStorage
    {
        [ObsoleteEx(Message = "The UseCloudTableClient method is deprecated in favor of the UseTableServiceClient method, which supports the TableServiceClient API provided by Azure.Data.Tables.", TreatAsErrorFromVersion = "5.0.0", RemoveInVersion = "6.0.0")]
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Subscriptions> UseCloudTableClient(
            this PersistenceExtensions<AzureTablePersistence, StorageType.Subscriptions> config,
            object client) =>
            throw new NotImplementedException();
    }
}
#pragma warning restore CS1591