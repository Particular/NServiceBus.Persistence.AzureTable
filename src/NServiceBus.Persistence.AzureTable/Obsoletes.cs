namespace NServiceBus.Persistence.AzureTable
{
    using System;

    /// <summary>
    /// Provides a CloudTableClient for the saga and outbox storage type via dependency injection. A custom implementation can be registered on the container and will be picked up by the persistence.
    /// </summary>
    [ObsoleteEx(Message = "The IProvideCloudTableClient is deprecated in favor of IProvideTableServiceClient, which supports the TableServiceClient API provided by Azure.Data.Tables.", TreatAsErrorFromVersion = "5.0.0", RemoveInVersion = "6.0.0")]
    public interface IProvideCloudTableClient
    {
        /// <summary>
        /// The CloudTableClient to use.
        /// </summary>
        object Client { get; }
    }

    /// <summary>
    /// Provides a CloudTableClient for the subscription storage type via dependency injection. A custom implementation can be registered on the container and will be picked up by the persistence.
    /// </summary>
    [ObsoleteEx(Message = "The IProvideCloudTableClientForSubscriptions is deprecated in favor of IProvideTableServiceClientForSubscriptions, which supports the TableServiceClient API provided by Azure.Data.Tables.", TreatAsErrorFromVersion = "5.0.0", RemoveInVersion = "6.0.0")]
    public interface IProvideCloudTableClientForSubscriptions
    {
        /// <summary>
        /// The CloudTableClient to use.
        /// </summary>
        object Client { get; }
    }

    /// <summary>
    /// Exception that is thrown when the table batch failed without throwing a storage exception. The exception gives access to the
    /// TableResult that exposes more details about the reason of failure.
    /// </summary>
    [ObsoleteEx(Message = "This exception class is deprecated in favor AzureTableBatchOperationException, supporting the Exception information exposed by Azure.Data.Tables.", TreatAsErrorFromVersion = "5.0.0", RemoveInVersion = "6.0.0")]
    public class TableBatchOperationException : Exception
    {
        /// <summary>
        /// Initializes a new TableBatchOperationException with a TableResult.
        /// </summary>
        public TableBatchOperationException(object result)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The TableResult exposing details about the reason of failure.
        /// </summary>
        public object Result => throw new NotImplementedException();
    }

    public partial interface IAzureTableStorageSession
    {
        /// <summary>
        /// The table that will be used to store the batched items.
        /// </summary>
        [ObsoleteEx(Message = "The Table property is deprecated in favor of the TableClient method, which supports the TableServiceClient API provided by Azure.Data.Tables.", TreatAsErrorFromVersion = "5.0.0", RemoveInVersion = "6.0.0")]
        object Table { get; }

        /// <summary>
        /// The transactional batch that can be used to store items.
        /// </summary>
        /// <remarks>The transactional batch exposed does delay the actual batch operations up to the point when the storage
        /// session is actually committed to avoiding running into transaction timeouts unnecessarily.</remarks>
        [ObsoleteEx(Message = "The Batch property is deprecated in favor of the BatchOperation method, which supports the TableServiceClient API provided by Azure.Data.Tables.", TreatAsErrorFromVersion = "5.0.0", RemoveInVersion = "6.0.0")]
        object Batch { get; }
    }

    partial class StorageSession
    {
        // This is just here to implement the full interface while we deprecate some properties

        /// <summary>
        /// The table that will be used to store the batched items.
        /// </summary>
        public object Table => throw new NotImplementedException();
        /// <summary>
        /// The transactional batch that can be used to store items.
        /// </summary>
        /// <remarks>The transactional batch exposed does delay the actual batch operations up to the point when the storage
        /// session is actually committed to avoiding running into transaction timeouts unnecessarily.</remarks>
        public object Batch => throw new NotImplementedException();
    }

    partial class AzureStorageSynchronizedStorageSession
    {
        // This is just here to implement the full interface while we deprecate some properties

        /// <summary>
        /// The table that will be used to store the batched items.
        /// </summary>
        public object Table => throw new NotImplementedException();
        /// <summary>
        /// The transactional batch that can be used to store items.
        /// </summary>
        /// <remarks>The transactional batch exposed does delay the actual batch operations up to the point when the storage
        /// session is actually committed to avoiding running into transaction timeouts unnecessarily.</remarks>
        public object Batch => throw new NotImplementedException();
    }
}

namespace NServiceBus.Testing
{
    using System;

    public partial class TestableAzureTableStorageSession
    {
        // This is just here to implement the full interface while we deprecate some properties

        /// <summary>
        /// The table that will be used to store the batched items.
        /// </summary>
        public object Table => throw new NotImplementedException();
        /// <summary>
        /// The transactional batch that can be used to store items.
        /// </summary>
        /// <remarks>The transactional batch exposed does delay the actual batch operations up to the point when the storage
        /// session is actually committed to avoiding running into transaction timeouts unnecessarily.</remarks>
        public object Batch => throw new NotImplementedException();
    }
}

namespace NServiceBus
{
    using System;

    public static partial class ConfigureAzureSagaStorage
    {
        /// <summary>
        /// Cloud Table Client to use for Saga, Outbox and Subscription storage.
        /// </summary>
        [ObsoleteEx(Message = "The UseCloudTableClient method is deprecated in favor of the UseTableServiceClient method, which supports the TableServiceClient API provided by Azure.Data.Tables.", TreatAsErrorFromVersion = "5.0.0", RemoveInVersion = "6.0.0")]
        public static PersistenceExtensions<AzureTablePersistence> UseCloudTableClient(
            this PersistenceExtensions<AzureTablePersistence> config, object client)
        {
            throw new NotImplementedException();
        }
    }

    public static partial class ConfigureAzureSubscriptionStorage
    {
        /// <summary>
        /// Cloud Table Client to use for the Subscription storage.
        /// </summary>
        [ObsoleteEx(Message = "The UseCloudTableClient method is deprecated in favor of the UseTableServiceClient method, which supports the TableServiceClient API provided by Azure.Data.Tables.", TreatAsErrorFromVersion = "5.0.0", RemoveInVersion = "6.0.0")]
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Subscriptions> UseCloudTableClient(
            this PersistenceExtensions<AzureTablePersistence, StorageType.Subscriptions> config,
            object client)
        {
            throw new NotImplementedException();
        }
    }
}