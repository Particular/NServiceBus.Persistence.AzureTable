namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Subscriptions
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;
    using NUnit.Framework;
    using ObjectApproval;

    [TestFixture]
    [Category("AzureStoragePersistence")]
    public class When_subscribing
    {
        [SetUp]
        public void Setup()
        {
            SubscriptionTestHelper.PerformStorageCleanup();
        }

        [Test]
        public async Task ensure_that_the_subscription_is_persisted()
        {
            var persister = SubscriptionTestHelper.CreateAzureSubscriptionStorage();
            var messageType = new MessageType(typeof(TestMessage));
            await persister.Subscribe(new Subscriber("address://test-queue", "endpointName"), messageType, null).ConfigureAwait(false);

            var subscribers = await persister.GetSubscribers(messageType).ConfigureAwait(false);

            Assert.That(subscribers.Count(), Is.EqualTo(1));

            var subscription = subscribers.ToArray()[0];
            Assert.That(subscription.TransportAddress, Is.EqualTo("address://test-queue"));
            Assert.That(subscription.Endpoint, Is.EqualTo("endpointName"));
        }

        [Test]
        public async Task ensure_that_the_subscription_is_version_ignorant()
        {
            var persister = SubscriptionTestHelper.CreateAzureSubscriptionStorage();

            var name = typeof(TestMessage).FullName;

            var messageTypes = new[]
            {
                new MessageType(name, new Version(1,2,3)),
                new MessageType(name, new Version(4,2,3)),
            };

            foreach (var messageType in messageTypes)
            {
                await persister.Subscribe(new Subscriber("address://test-queue", "endpointName"), messageType, null).ConfigureAwait(false);
            }

            var subscribers = await persister.GetSubscribers(new MessageType(typeof(TestMessage))).ConfigureAwait(false);

            Assert.That(subscribers.Count(), Is.EqualTo(1));

            var subscription = subscribers.ToArray()[0];
            Assert.That(subscription.TransportAddress, Is.EqualTo("address://test-queue"));
            Assert.That(subscription.Endpoint, Is.EqualTo("endpointName"));
        }

        [Test]
        public async Task ensure_that_the_subscription_selects_proper_message_types()
        {
            var persister = SubscriptionTestHelper.CreateAzureSubscriptionStorage();

            await persister.Subscribe(new Subscriber("address://test-queue", "endpointName"), new MessageType(typeof(TestMessage)), new ContextBag()).ConfigureAwait(false);
            await persister.Subscribe(new Subscriber("address://test-queue2", "endpointName"), new MessageType(typeof(TestMessagea)), new ContextBag()).ConfigureAwait(false);

            var subscribers = await persister.GetSubscribers(new MessageType(typeof(TestMessage))).ConfigureAwait(false);

            Assert.That(subscribers.Count(), Is.EqualTo(1));

            var subscription = subscribers.ToArray()[0];
            Assert.That(subscription.TransportAddress, Is.EqualTo("address://test-queue"));
            Assert.That(subscription.Endpoint, Is.EqualTo("endpointName"));
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
            VerifyCache(persister.Cache);
        }

        [Test]
        public void Subscribe_with_same_type_should_clear_cache()
        {
            var persister = SubscriptionTestHelper.CreateAzureSubscriptionStorage();
            var matchingType = new MessageType("matchingType", new Version(0, 0, 0, 0));
            persister.Subscribe(new Subscriber("address://test-queue", "endpoint"), matchingType, null).Await();
            persister.GetSubscribers(matchingType).Await();
            persister.Subscribe(new Subscriber("address://test-queue", "endpoint"), matchingType, null).Await();
            VerifyCache(persister.Cache);
        }

        [Test]
        public void Unsubscribe_with_same_type_should_clear_cache()
        {
            var persister = SubscriptionTestHelper.CreateAzureSubscriptionStorage();
            var matchingType = new MessageType("matchingType", new Version(0, 0, 0, 0));
            persister.Subscribe(new Subscriber("address://test-queue", "endpoint"), matchingType, null).Await();
            persister.GetSubscribers(matchingType).Await();
            persister.Unsubscribe(new Subscriber("address://test-queue", "endpoint"), matchingType, null).Await();
            VerifyCache(persister.Cache);
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
            VerifyCache(persister.Cache);
        }

        static void VerifyCache(ConcurrentDictionary<string, AzureSubscriptionStorage.CacheItem> cache)
        {
            var items = cache
                .OrderBy(_ => _.Key)
                .ToDictionary(_ => _.Key,
                    elementSelector: item =>
                    {
                        return item.Value.Subscribers.Result
                            .OrderBy(_ => _.Endpoint)
                            .ThenBy(_ => _.TransportAddress);
                    });
            ObjectApprover.VerifyWithJson(items);
        }
    }

    class TestMessage
    {
    }

    class TestMessagea
    {
    }
}
