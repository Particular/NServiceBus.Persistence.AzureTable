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
        /// - SagaId header if present -or-
        /// - The SagaId on the secondary index derived from the correlation property information when the migration mode is enabled -or-
        /// - The deterministic SagaId precalculated by using the correlation property information
        /// </summary>
        /// <param name="context">The logical message handler context.</param>
        /// <param name="correlationProperty">The correlation property information derived from the logical message.</param>
        /// <exception cref="Exception">When the saga id could not be determined by the saga id header and <see cref="SagaCorrelationProperty.None"/> is passed.</exception>
        Task SetPartitionKey<TSagaData>(IIncomingLogicalMessageContext context,
            SagaCorrelationProperty correlationProperty)
            where TSagaData : IContainSagaData;
    }
}