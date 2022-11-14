namespace NServiceBus.Persistence.AzureTable
{
    using Extensibility;

    class TableClientHolderResolver
    {
        readonly IProvideTableServiceClient provideTableServiceClient;
        readonly TableInformation? defaultTableInformation;

        public TableClientHolderResolver(IProvideTableServiceClient provideTableServiceClient, TableInformation? defaultTableInformation)
        {
            this.defaultTableInformation = defaultTableInformation;
            this.provideTableServiceClient = provideTableServiceClient;
        }

        public TableClientHolder ResolveAndSetIfAvailable(ContextBag context)
        {
            if (context.TryGet<TableClientHolder>(out var tableHolder))
            {
                return tableHolder;
            }

            var information = context.TryGet<TableInformation>(out var tableInformation) ? tableInformation : defaultTableInformation;

            if (!information.HasValue)
            {
                return null;
            }

            var informationValue = information.Value;
            tableHolder = new TableClientHolder(provideTableServiceClient.Client.GetTableClient(informationValue.TableName));
            context.Set(tableHolder);
            return tableHolder;
        }
    }
}