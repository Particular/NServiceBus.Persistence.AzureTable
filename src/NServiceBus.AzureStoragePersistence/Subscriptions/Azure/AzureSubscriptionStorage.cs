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
    using Microsoft.WindowsAzure.Storage.Table.Queryable;
    using NServiceBus.Azure;
    using Extensibility;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
    using NServiceBus.Routing;

    /// <summary>
    /// Provides Azure Storage Table storage functionality for Subscriptions
    /// </summary>
    public class AzureSubscriptionStorage : ISubscriptionStorage
    {
        readonly string subscriptionTableName;
        CloudTableClient client;

        /// <summary>
        /// </summary>
        /// <param name="subscriptionTableName">Table name used to store subscription information</param>
        /// <param name="subscriptionConnectionString">Subscription connection string</param>
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
        /// Stores a subscription
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
                    PartitionKey = messageType.ToString(),
                    RowKey = EncodeTo64(subscriber.Endpoint.ToString()),
                    TransportAddress = subscriber.TransportAddress
                };

                var operation = TableOperation.Insert(subscription);
                await table.ExecuteAsync(operation).ConfigureAwait(true);
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
        /// <param name="context">The current context</param>
        public async Task Unsubscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
        {
            var table = client.GetTableReference(subscriptionTableName);
            var encodedAddress = EncodeTo64(subscriber.Endpoint.ToString());

            var query = from s in table.CreateQuery<Subscription>()
                        where s.PartitionKey == messageType.ToString() && s.RowKey == encodedAddress && s.TransportAddress == subscriber.TransportAddress
                        select s;

            var subscription = query.AsTableQuery().AsEnumerable().SafeFirstOrDefault();
            if (subscription != null)
            {
                var operation = TableOperation.Delete(subscription);
                await table.ExecuteAsync(operation).ConfigureAwait(true);
            }
        }

        /// <summary>
        /// Returns the subscription address based on message type
        /// </summary>
        /// <param name="messageTypes">Types of messages that subscription addresses should be found for</param>
        /// <returns>Subscription addresses that were found for the provided messageTypes</returns>
        /// <param name="context">The current context</param>
        public Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context)
        {
            var subscribers = new List<Subscriber>();
            var table = client.GetTableReference(subscriptionTableName);

            foreach (var messageType in messageTypes)
            {
                var query = from s in table.CreateQuery<Subscription>()
                            where s.PartitionKey == messageType.ToString()
                            select new Subscriber(s.TransportAddress, new EndpointName(DecodeFrom64(s.RowKey)));

                subscribers.AddRange(query.ToList());
            }

            return Task.FromResult((IEnumerable<Subscriber>)subscribers);
        }
    }
}