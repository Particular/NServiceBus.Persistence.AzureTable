namespace NServiceBus.TransactionalSession
{
    using Persistence.AzureTable;

    sealed class SetAsDispatchedHolderOpenSessionOptionCustomization : IOpenSessionOptionsCustomization
    {
        public SetAsDispatchedHolderOpenSessionOptionCustomization(TableClientHolderResolver tableClientHolderResolver) =>
            this.tableClientHolderResolver = tableClientHolderResolver;

        public void Apply(OpenSessionOptions options)
        {
            if (options is AzureTableOpenSessionOptions azureTableOpenSessionOptions)
            {
                azureTableOpenSessionOptions.SetDispatchHolder(tableClientHolderResolver);
            }
        }

        readonly TableClientHolderResolver tableClientHolderResolver;
    }
}