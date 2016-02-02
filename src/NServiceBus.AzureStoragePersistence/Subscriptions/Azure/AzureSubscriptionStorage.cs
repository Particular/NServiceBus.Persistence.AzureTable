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
    /// </summary>
    public class AzureSubscriptionStorage : ISubscriptionStorage
    {
        private readonly string subscriptionTableName;
        private CloudTableClient client;

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
            var toEncodeAsBytes = Encoding.ASCII.GetBytes(toEncode);
            var returnValue = Convert.ToBase64String(toEncodeAsBytes);
            return returnValue;
        }

        static string DecodeFrom64(string encodedData)
        {
            var encodedDataAsBytes = Convert.FromBase64String(encodedData);
            var returnValue = Encoding.ASCII.GetString(encodedDataAsBytes);
            return returnValue;
        }
    }
}