namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Subscriptions
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    [TestFixture]
    [Category("AzureStoragePersistence")]
    public class When_caching
    {
        [SetUp]
        public void Setup()
        {
            SubscriptionTestHelper.PerformStorageCleanup();
        }

        [Test]
        public async Task Cached_get_should_be_faster()
        {
            var persister = SubscriptionTestHelper.CreateAzureSubscriptionStorage();
            var type = new MessageType("type1", new Version(0, 0, 0, 0));
            persister.Subscribe(new Subscriber("address://test-queue", "endpoint"), type, null).Await();
            var first = Stopwatch.StartNew();
            var subscribersFirst = await persister.GetSubscribers(type)
                .ConfigureAwait(false);
            var firstTime = first.ElapsedMilliseconds;
            var second = Stopwatch.StartNew();
            var subscribersSecond = await persister.GetSubscribers(type)
                .ConfigureAwait(false);
            var secondTime = second.ElapsedMilliseconds;

            Assert.IsTrue(secondTime * 1000 < firstTime);
            Assert.AreEqual(subscribersFirst.Count(), subscribersSecond.Count());
        }

        [Test]
        public void Should_be_cached()
        {
            var persister = SubscriptionTestHelper.CreateAzureSubscriptionStorage();
            var type = new MessageType("type1", new Version(0, 0, 0, 0));
            persister.Subscribe(new Subscriber("address://test-queue", "endpoint"), type, null).Await();
            persister.GetSubscribers(type).Await();

            var cache = persister.Cache;
            Assert.AreEqual(cache.Count, 1, "should only contain one item");

            var subscribers = cache["type1,"].Subscribers.Result;
            Assert.AreEqual(subscribers.Count(), 1, "should only contain one subscriber");

            var subscriber = subscribers.First();
            Assert.AreEqual(subscriber.TransportAddress, "address://test-queue");
            Assert.AreEqual(subscriber.Endpoint, "endpoint");
        }

        [Test]
        public void Subscribe_with_same_type_should_clear_cache()
        {
            var persister = SubscriptionTestHelper.CreateAzureSubscriptionStorage();
            var matchingType = new MessageType("matchingType", new Version(0, 0, 0, 0));
            persister.Subscribe(new Subscriber("address://test-queue", "endpoint"), matchingType, null).Await();
            persister.GetSubscribers(matchingType).Await();
            persister.Subscribe(new Subscriber("address://test-queue", "endpoint"), matchingType, null).Await();

            var cache = persister.Cache;
            Assert.AreEqual(cache.Count, 0, "should not contain any items");
        }

        [Test]
        public void Unsubscribe_with_same_type_should_clear_cache()
        {
            var persister = SubscriptionTestHelper.CreateAzureSubscriptionStorage();
            var matchingType = new MessageType("matchingType", new Version(0, 0, 0, 0));
            persister.Subscribe(new Subscriber("address://test-queue", "endpoint"), matchingType, null).Await();
            persister.GetSubscribers(matchingType).Await();
            persister.Unsubscribe(new Subscriber("address://test-queue", "endpoint"), matchingType, null).Await();

            var cache = persister.Cache;
            Assert.AreEqual(cache.Count, 0, "should not contain any items");
        }

        [Test]
        public void Unsubscribe_with_part_type_should_partially_clear_cache()
        {
            var persister = SubscriptionTestHelper.CreateAzureSubscriptionStorage();
            var version = new Version(0, 0, 0, 0);
            var type1 = new MessageType("type1", version);
            var type2 = new MessageType("type2", version);
            var type3 = new MessageType("type3", version);
            persister.Subscribe(new Subscriber("address://test-queue", "endpoint"), type1, null).Await();
            persister.Subscribe(new Subscriber("address://test-queue", "endpoint"), type2, null).Await();
            persister.Subscribe(new Subscriber("address://test-queue", "endpoint"), type3, null).Await();

            persister.GetSubscribers(type1).Await();
            persister.GetSubscribers(type2).Await();
            persister.GetSubscribers(type3).Await();
            persister.GetSubscribers(type1, type2).Await();
            persister.GetSubscribers(type2, type3).Await();
            persister.GetSubscribers(type1, type3).Await();
            persister.GetSubscribers(type1, type2, type3).Await();
            persister.Unsubscribe(new Subscriber("address://test-queue", "endpoint"), type2, null).Await();

            var cache = persister.Cache;
            Assert.AreEqual(cache.Count, 3, "should contain 3 items exactly");

            // subscribers for "type1"
            var subscribers = cache["type1,"].Subscribers.Result.ToArray();
            Assert.AreEqual(subscribers.Length, 1, "should only contain one subscriber");

            var subscriber = subscribers[0];
            Assert.AreEqual(subscriber.TransportAddress, "address://test-queue");
            Assert.AreEqual(subscriber.Endpoint, "endpoint");

            // subscribers for "type1,type3,"
            subscribers = cache["type1,type3,"].Subscribers.Result.ToArray();
            Assert.AreEqual(subscribers.Length, 1, "should only contain one subscriber");

            subscriber = subscribers[0];
            Assert.AreEqual(subscriber.TransportAddress, "address://test-queue");
            Assert.AreEqual(subscriber.Endpoint, "endpoint");

            // subscribers for "type3,"
            subscribers = cache["type3,"].Subscribers.Result.ToArray();
            Assert.AreEqual(subscribers.Length, 1, "should only contain one subscriber");

            subscriber = subscribers[0];
            Assert.AreEqual(subscriber.TransportAddress, "address://test-queue");
            Assert.AreEqual(subscriber.Endpoint, "endpoint");
        }
    }
}