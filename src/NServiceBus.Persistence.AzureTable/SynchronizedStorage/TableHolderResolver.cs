namespace NServiceBus.Persistence.AzureTable
{
    using Extensibility;

    class TableHolderResolver
    {
        readonly IProvideTableServiceClient provideTableServiceClient;
        readonly TableInformation? defaultTableInformation;

        public TableHolderResolver(IProvideTableServiceClient provideTableServiceClient, TableInformation? defaultTableInformation)
        {
            this.defaultTableInformation = defaultTableInformation;
            this.provideTableServiceClient = provideTableServiceClient;
        }

        public TableHolder ResolveAndSetIfAvailable(ContextBag context)
        {
            if (context.TryGet<TableHolder>(out var tableHolder))
            {
                return tableHolder;
            }

            var information = context.TryGet<TableInformation>(out var tableInformation) ? tableInformation : defaultTableInformation;

            if (!information.HasValue)
            {
                return null;
            }

            var informationValue = information.Value;
            tableHolder = new TableHolder(provideTableServiceClient.Client.GetTableClient(informationValue.TableName));
            context.Set(tableHolder);
            return tableHolder;
        }
    }
}