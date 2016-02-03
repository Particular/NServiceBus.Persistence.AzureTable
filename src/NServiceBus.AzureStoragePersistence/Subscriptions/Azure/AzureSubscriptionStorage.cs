namespace NServiceBus.Unicast.Subscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using Microsoft.WindowsAzure.Storage.Table;
    using Microsoft.WindowsAzure.Storage.Table.Queryable;
    using NServiceBus.Azure;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

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

        /// <summary>
        /// Stores a subscription
        /// </summary>
        /// <param name="address">The address that is being subscribed to</param>
        /// <param name="messageTypes">The types of messages that are being subscribed to</param>
        void ISubscriptionStorage.Subscribe(Address address, IEnumerable<MessageType> messageTypes)
        {
            var table = client.GetTableReference(subscriptionTableName);

            foreach (var messageType in messageTypes)
            {
                try
                {
                    var subscription = new Subscription
                    {
                        RowKey = EncodeTo64(address.ToString()),
                        PartitionKey = messageType.ToString()
                    };
                    var operation = TableOperation.Insert(subscription);
                    table.Execute(operation);
                }
                catch (StorageException ex)
                {
                    if (ex.RequestInformation.HttpStatusCode != 409)
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Removes a subscription
        /// </summary>
        /// <param name="address">The address that is being unsubscribed from</param>
        /// <param name="messageTypes">The types of messages that are being unsubscribed</param>
        void ISubscriptionStorage.Unsubscribe(Address address, IEnumerable<MessageType> messageTypes)
        {
            var table = client.GetTableReference(subscriptionTableName);

            var encodedAddress = EncodeTo64(address.ToString());
            foreach (var messageType in messageTypes)
            {
                var type = messageType;
                var query = from s in table.CreateQuery<Subscription>()
                    where s.PartitionKey == type.ToString() && s.RowKey == encodedAddress
                    select s;
                var subscription = query.AsTableQuery().AsEnumerable().SafeFirstOrDefault();
                if (subscription != null)
                {
                    var operation = TableOperation.Delete(subscription);
                    table.Execute(operation);
                }
            }
        }

        /// <summary>
        /// Returns the subscription address based on message type
        /// </summary>
        /// <param name="messageTypes">Types of messages that subscription addresses should be found for</param>
        /// <returns>Subscription addresses that were found for the provided messageTypes</returns>
        IEnumerable<Address> ISubscriptionStorage.GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes)
        {
            var subscribers = new List<Address>();
            var table = client.GetTableReference(subscriptionTableName);

            foreach (var messageType in messageTypes)
            {
                var type = messageType;
                var query = from s in table.CreateQuery<Subscription>()
                    where s.PartitionKey == type.ToString()
                    select s;

                var result = query.ToList();

                subscribers.AddRange(result.Select(s => Address.Parse(DecodeFrom64(s.RowKey))));
            }

            return subscribers;
        }

        public void Init()
        {
            //No-op
        }

        static string EncodeTo64(string toEncode)
        {
            return Convert.ToBase64String(Encoding.ASCII.GetBytes(toEncode));
        }

        static string DecodeFrom64(string encodedData)
        {
            return Encoding.ASCII.GetString(Convert.FromBase64String(encodedData));
        }
    }
}