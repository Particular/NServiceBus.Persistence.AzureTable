namespace NServiceBus.Persistence.AzureTable
{
    using System.Net;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Extensibility;
    using Microsoft.Azure.Cosmos.Table;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;


    class AzureSubscriptionStorage : ISubscriptionStorage
    {
        string subscriptionTableName;
        TimeSpan? cacheFor;
        CloudTableClient client;
        public ConcurrentDictionary<string, CacheItem> Cache;

        public AzureSubscriptionStorage(IProvideCloudTableClientForSubscriptions tableClientProvider, string subscriptionTableName, TimeSpan? cacheFor)
        {
            this.subscriptionTableName = subscriptionTableName;
            this.cacheFor = cacheFor;
            client = tableClientProvider.Client;

            if (cacheFor != null)
            {
                Cache = new ConcurrentDictionary<string, CacheItem>();
            }
        }

        static string EncodeTo64(string toEncode)
        {
            return Convert.ToBase64String(Encoding.ASCII.GetBytes(toEncode));
        }

        static string DecodeFrom64(string encodedData)
        {
            return Encoding.ASCII.GetString(Convert.FromBase64String(encodedData));
        }

        public async Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
        {
            var table = client.GetTableReference(subscriptionTableName);

            var subscription = new Subscription
            {
                RowKey = EncodeTo64(subscriber.TransportAddress),
                PartitionKey = messageType.ToString(),
                EndpointName = subscriber.Endpoint,
                ETag = "*"
            };

            try
            {
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
            ClearForMessageType(messageType);
        }

        public async Task Unsubscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
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
                var operation = TableOperation.Delete(subscription);
                await table.ExecuteAsync(operation).ConfigureAwait(false);
            }
            catch (StorageException ex) when(ex.RequestInformation.HttpStatusCode == (int) HttpStatusCode.NotFound)
            {
                // intentionally ignored
            }
            ClearForMessageType(messageType);
        }

        static string GetKey(List<MessageType> types)
        {
            var typeNames = types.Select(_ => _.TypeName);
            return string.Join(",", typeNames) + ",";
        }

        static string GetKeyPart(MessageType type)
        {
            return $"{type.TypeName},";
        }

        void ClearForMessageType(MessageType messageType)
        {
            if (cacheFor == null)
            {
                return;
            }
            var keyPart = GetKeyPart(messageType);
            foreach (var cacheKey in Cache.Keys)
            {
                if (cacheKey.Contains(keyPart))
                {
                    Cache.TryRemove(cacheKey, out _);
                }
            }
        }

        public Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context)
        {
            var types = messageTypes.ToList();

            if (cacheFor == null)
            {
                return GetSubscriptions(types);
            }

            var key = GetKey(types);

            if (Cache.TryGetValue(key, out var cacheItem))
            {
                var age = DateTime.UtcNow - cacheItem.Stored;
                if (age >= cacheFor)
                {
                    var subscriptions = GetSubscriptions(types);
                    Cache[key] = new CacheItem
                    {
                        Stored = DateTime.UtcNow,
                        Subscribers = subscriptions
                    };
                    return subscriptions;
                }
                return cacheItem.Subscribers;
            }
            else
            {
                var subscriptions = GetSubscriptions(types);
                Cache[key] = new CacheItem
                {
                    Stored = DateTime.UtcNow,
                    Subscribers = subscriptions
                };
                return subscriptions;
            }
        }

        async Task<IEnumerable<Subscriber>> GetSubscriptions(IEnumerable<MessageType> messageTypes)
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

        internal class CacheItem
        {
            public DateTime Stored;
            public Task<IEnumerable<Subscriber>> Subscribers;
        }
    }
}