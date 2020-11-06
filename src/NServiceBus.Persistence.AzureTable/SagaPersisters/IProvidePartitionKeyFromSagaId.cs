namespace NServiceBus.Persistence.AzureTable
{
    using System.Threading.Tasks;
    using Pipeline;
    using Sagas;

    /// <summary>
    /// TODO
    /// </summary>
    public interface IProvidePartitionKeyFromSagaId
    {
        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="context">TODO TODO</param>
        /// <param name="correlationProperty">TODO TODO TODO</param>
        Task SetPartitionKey<TSagaData>(IIncomingLogicalMessageContext context,
            SagaCorrelationProperty correlationProperty)
            where TSagaData : IContainSagaData;
    }
}