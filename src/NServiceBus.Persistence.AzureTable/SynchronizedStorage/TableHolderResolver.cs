namespace NServiceBus.Persistence.AzureTable
{
    using Extensibility;

    class TableHolderResolver
    {
        readonly IProvideCloudTableClient provideCloudTableClient;
        readonly TableInformation? defaultTableInformation;

        public TableHolderResolver(IProvideCloudTableClient provideCloudTableClient, TableInformation? defaultTableInformation)
        {
            this.defaultTableInformation = defaultTableInformation;
            this.provideCloudTableClient = provideCloudTableClient;
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
            tableHolder = new TableHolder(provideCloudTableClient.Client.GetTableReference(informationValue.TableName));
            context.Set(tableHolder);
            return tableHolder;
        }
    }
}