namespace NServiceBus.Persistence.AzureTable
{
    using System;

    /// <summary>
    /// Extensions for the <see cref="SynchronizedStorageSession"/>.
    /// </summary>
    public static class SynchronizedStorageSessionExtensions
    {
        /// <summary>
        /// Retrieves the shared <see cref="IAzureTableStorageSession"/> from the <see cref="SynchronizedStorageSession"/>.
        /// </summary>
        public static IAzureTableStorageSession AzureTablePersistenceSession(
            this ISynchronizedStorageSession session)
        {
            Guard.AgainstNull(nameof(session), session);

            if (session is not IWorkWithSharedTransactionalBatch workWith)
            {
                throw new Exception($"Cannot access the synchronized storage session. Ensure that 'EndpointConfiguration.UsePersistence<{nameof(AzureTablePersistence)}>()' has been called.");
            }

            if (!workWith.CurrentContextBag.TryGet<TableEntityPartitionKey>(out _))
            {
                throw new Exception("To use the shared transactional batch a partition key must be set using a custom pipeline behavior.");
            }

            return workWith;
        }
    }
}