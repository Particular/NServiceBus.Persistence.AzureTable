namespace NServiceBus.Persistence.AzureStorage
{
    using Microsoft.Azure.Cosmos.Table;

    class CloudTableClientForSubscriptionsFromConnectionString : IProvideCloudTableClientForSubscriptions
    {
        public CloudTableClientForSubscriptionsFromConnectionString(string subscriptionConnectionString)
        {
            var subscriptionAccount = CloudStorageAccount.Parse(subscriptionConnectionString);
            Client = subscriptionAccount.CreateCloudTableClient();
            Client.DefaultRequestOptions = new TableRequestOptions
            {
                RetryPolicy = new ExponentialRetry()
            };
        }

        public CloudTableClient Client { get; }
    }
}