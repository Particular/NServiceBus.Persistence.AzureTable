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
    using NServiceBus.Azure;
    using Extensibility;
    using NServiceBus.Routing;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

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
                    EndpointName = subscriber.Endpoint.ToString()
                };

                var operation = TableOperation.Insert(subscription);
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

            var subscription = (await table.ExecuteAsync(retrieveOperation).ConfigureAwait(false)).Result as TimeoutDataEntity;
            if (subscription != null)
            {
                var operation = TableOperation.Delete(subscription);
                await table.ExecuteAsync(operation).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Returns the subscription address based on message type
        /// </summary>
        /// <param name="messageTypes">Types of messages that subscription addresses should sbe found for</param>
        /// <param name="context">The current pipeline context</param>
        /// <returns>Subscription addresses that were found for the provided messageTypes</returns>
        public async Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context)
        {
            var subscribers = new List<Subscriber>();
            var table = client.GetTableReference(subscriptionTableName);

            foreach (var messageType in messageTypes)
            {
                var query = new TableQuery<Subscription>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, messageType.ToString()));

                var results = (await table.ExecuteQueryAsync(query).ConfigureAwait(false)).Select(s => new Subscriber(DecodeFrom64(s.RowKey), new EndpointName(s.EndpointName)));

                subscribers.AddRange(results);
            }

            return subscribers;
        }
    }
}