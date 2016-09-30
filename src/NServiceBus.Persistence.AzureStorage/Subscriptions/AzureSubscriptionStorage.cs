namespace NServiceBus.Unicast.Subscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using Microsoft.WindowsAzure.Storage.Table;
    using Extensibility;
    using MessageDrivenSubscriptions;
    using Persistence.AzureStorage;

    class AzureSubscriptionStorage : ISubscriptionStorage
    {
        string subscriptionTableName;
        CloudTableClient client;

        public AzureSubscriptionStorage(string subscriptionTableName, string subscriptionConnectionString)
        {
            this.subscriptionTableName = subscriptionTableName;
            var account = CloudStorageAccount.Parse(subscriptionConnectionString);

            client = account.CreateCloudTableClient();
            client.DefaultRequestOptions = new TableRequestOptions
            {
                RetryPolicy = new ExponentialRetry()
            };
        }

        static string EncodeTo64(string toEncode)
        {
            return Convert.ToBase64String(Encoding.ASCII.GetBytes(toEncode));
        }

        static string DecodeFrom64(string encodedData)
        {
            return Encoding.ASCII.GetString(Convert.FromBase64String(encodedData));
        }

        /// <summary>
        /// Subscribes the given client to messages of a given type.
        /// </summary>
        /// <param name="subscriber">The subscriber to subscribe</param>
        /// <param name="messageType">The types of messages that are being subscribed to</param>
        /// <param name="context">The current context</param>
        public async Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
        {
            var table = client.GetTableReference(subscriptionTableName);

            try
            {
                var subscription = new Subscription
                {
                    RowKey = EncodeTo64(subscriber.TransportAddress),
                    PartitionKey = messageType.ToString(),
                    EndpointName = subscriber.Endpoint,
                    ETag = "*"
                };

                var operation = TableOperation.InsertOrReplace(subscription);
                await table.ExecuteAsync(operation).ConfigureAwait(false);
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode != 409)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Removes a subscription
        /// </summary>
        /// <param name="subscriber">The subscriber to unsubscribe</param>
        /// <param name="messageType">The message type to unsubscribed</param>
        /// <param name="context">The current pipeline context</param>
        public async Task Unsubscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
        {
            var table = client.GetTableReference(subscriptionTableName);
            var encodedAddress = EncodeTo64(subscriber.TransportAddress);

            var retrieveOperation = TableOperation.Retrieve<TimeoutDataEntity>(messageType.ToString(), encodedAddress);

            var tableResult = await table.ExecuteAsync(retrieveOperation).ConfigureAwait(false);
            var subscription = tableResult.Result as TimeoutDataEntity;
            if (subscription != null)
            {
                var operation = TableOperation.Delete(subscription);
                await table.ExecuteAsync(operation).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Returns the subscription address based on message type
        /// </summary>
        /// <param name="messageTypes">Types of messages that subscription addresses should be found for</param>
        /// <param name="context">The current pipeline context</param>
        /// <returns>Subscription addresses that were found for the provided messageTypes</returns>
        public async Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context)
        {
            var subscribers = new HashSet<Subscriber>(SubscriberComparer.Instance);
            var table = client.GetTableReference(subscriptionTableName);

            foreach (var messageType in messageTypes)
            {
                var name = messageType.TypeName;
                var lowerBound = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.GreaterThanOrEqual, name);
                var upperBound = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.LessThan, GetUpperBound(name));
                var query = new TableQuery<Subscription>().Where(TableQuery.CombineFilters(lowerBound, "and", upperBound));

                var subscriptions = await table.ExecuteQueryAsync(query).ConfigureAwait(false);
                var results = subscriptions.Select(s => new Subscriber(DecodeFrom64(s.RowKey), s.EndpointName));

                foreach (var subscriber in results)
                {
                    subscribers.Add(subscriber);
                }
            }

            return subscribers;
        }

        static string GetUpperBound(string name)
        {
            return name + ", Version=z";
        }

        class SubscriberComparer : IEqualityComparer<Subscriber>
        {
            public static readonly SubscriberComparer Instance = new SubscriberComparer();

            public bool Equals(Subscriber x, Subscriber y)
            {
                return StringComparer.InvariantCulture.Compare(x.Endpoint, y.Endpoint) == 0 && StringComparer.InvariantCulture.Compare(x.TransportAddress, y.TransportAddress) == 0;
            }

            public int GetHashCode(Subscriber obj)
            {
                return (obj.Endpoint ?? string.Empty).Length;
            }
        }
    }
}