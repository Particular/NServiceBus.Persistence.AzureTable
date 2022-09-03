namespace NServiceBus.TransactionalSession
{
    using Persistence.AzureTable;

    /// <summary>
    /// The options allowing to control the behavior of the transactional session.
    /// </summary>
    public sealed class AzureTableOpenSessionOptions : OpenSessionOptions
    {
        /// <summary>
        /// Creates a new instance of the CosmosOpenSessionOptions.
        /// </summary>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="tableInformation">The optional container information.</param>
        public AzureTableOpenSessionOptions(TableEntityPartitionKey partitionKey, TableInformation? tableInformation = null)
        {
            Extensions.Set(partitionKey);
            Metadata.Add(AzureTableControlMessageBehavior.PartitionKeyStringHeaderKey, partitionKey.PartitionKey);

            Extensions.Set(partitionKey);

            SetTableInformationIfRequired(tableInformation);
        }

        internal void SetTableInformationIfRequired(TableInformation? tableInformation)
        {
            if (!tableInformation.HasValue || Extensions.TryGet<TableInformation>(out _))
            {
                return;
            }

            Extensions.Set(tableInformation.Value);
            Metadata.Add(AzureTableControlMessageBehavior.TableInformationHeaderKey, tableInformation.Value.TableName);
        }

        internal void SetDispatchHolder(TableHolderResolver resolver) =>
            Extensions.Set(new SetAsDispatchedHolder
            {
                TableHolder = resolver.ResolveAndSetIfAvailable(Extensions),
                PartitionKey = Extensions.Get<TableEntityPartitionKey>()
            });
    }
}