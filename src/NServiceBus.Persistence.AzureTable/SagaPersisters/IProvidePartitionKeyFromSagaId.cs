namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Sagas;

    /// <summary>
    /// Provides the ability to derive the partition key from the saga id. This is helpful in scenarios when transactionality is required in conversations that involve
    /// a saga and potentially multiple message handlers as part of the same message handling pipeline or to achieve outbox behavior derived from the saga id.
    /// </summary>
    public interface IProvidePartitionKeyFromSagaId
    {
        /// <summary>
        /// Sets the partition key based on the
        /// - The saga ID header if present -or-
        /// - The saga ID on the secondary index derived from the specified <paramref name="correlationProperty"/> when compatibility mode is enabled -or-
        /// - The saga ID calculated using the specified <paramref name="correlationProperty"/>.
        /// </summary>
        /// <param name="context">The logical message handler context.</param>
        /// <param name="correlationProperty">The correlation property information derived from the logical message.</param>
        /// <exception cref="Exception">When the specified <paramref name="correlationProperty"/> is <see cref="SagaCorrelationProperty.None"/> and the saga ID header is not present.</exception>
        Task SetPartitionKey<TSagaData>(IIncomingLogicalMessageContext context,
            SagaCorrelationProperty correlationProperty)
            where TSagaData : IContainSagaData;
    }
}