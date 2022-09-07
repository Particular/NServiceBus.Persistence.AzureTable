namespace NServiceBus.TransactionalSession
{
    using Persistence.AzureTable;

    sealed class SetAsDispatchedHolderOpenSessionOptionCustomization : IOpenSessionOptionsCustomization
    {
        public SetAsDispatchedHolderOpenSessionOptionCustomization(TableHolderResolver tableHolderResolver) =>
            this.tableHolderResolver = tableHolderResolver;

        public void Apply(OpenSessionOptions options)
        {
            if (options is AzureTableOpenSessionOptions azureTableOpenSessionOptions)
            {
                azureTableOpenSessionOptions.SetDispatchHolder(tableHolderResolver);
            }
        }

        readonly TableHolderResolver tableHolderResolver;
    }
}