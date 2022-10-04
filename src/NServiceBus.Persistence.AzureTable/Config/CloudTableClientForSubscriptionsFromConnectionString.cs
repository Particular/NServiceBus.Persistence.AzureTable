namespace NServiceBus.Persistence.AzureTable
{
    using Azure.Core;
    using Azure.Data.Tables;

    class CloudTableClientForSubscriptionsFromConnectionString : IProvideCloudTableClientForSubscriptions
    {
        public CloudTableClientForSubscriptionsFromConnectionString(string subscriptionConnectionString)
        {
            // TODO: should we set additional options here?
            var tableClientOptions = new TableClientOptions
            {
                Retry = { Mode = RetryMode.Exponential }
            };
            Client = new TableServiceClient(subscriptionConnectionString, tableClientOptions);
        }

        public TableServiceClient Client { get; }
    }
}