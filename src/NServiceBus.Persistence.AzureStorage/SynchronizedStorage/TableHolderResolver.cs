namespace NServiceBus.Persistence.AzureStorage
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

            TableInformation? information;
            if (context.TryGet<TableInformation>(out var containerInformation))
            {
                information = containerInformation;
            }
            else
            {
                information = defaultTableInformation;
            }

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