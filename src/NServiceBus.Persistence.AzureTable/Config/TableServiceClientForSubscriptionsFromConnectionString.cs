namespace NServiceBus.Persistence.AzureTable
{
    using Azure.Core;
    using Azure.Data.Tables;

    class TableServiceClientForSubscriptionsFromConnectionString : IProvideTableServiceClientForSubscriptions
    {
        public TableServiceClientForSubscriptionsFromConnectionString(string subscriptionConnectionString)
        {
            var tableClientOptions = new TableClientOptions
            {
                Retry = { Mode = RetryMode.Exponential }
            };
            Client = new TableServiceClient(subscriptionConnectionString, tableClientOptions);
        }

        public TableServiceClient Client { get; }
    }
}