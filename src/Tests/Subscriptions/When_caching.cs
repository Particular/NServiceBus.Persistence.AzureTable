namespace NServiceBus.Persistence.AzureTable.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Particular.Approvals;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    [TestFixture("StorageTable")]
    [TestFixture("CosmosDB")]
    public class When_caching
    {
        string tableApiType;
        AzureSubscriptionStorage persister;
        SubscriptionTestHelper.Scope scope;

        public When_caching(string tableApiType) => this.tableApiType = tableApiType;

        [SetUp]
        public async Task Setup()
        {
            scope = await SubscriptionTestHelper.CreateAzureSubscriptionStorage(tableApiType);
            persister = scope.Storage;
        }

        [TearDown]
        public async Task Teardown() => await scope.DisposeAsync();

        [Test]
        public async Task Cached_get_should_be_faster()
        {
            var type = new MessageType("type1", new Version(0, 0, 0, 0));
            await persister.Subscribe(new Subscriber("address://test-queue", "endpoint"), type, null);
            var first = Stopwatch.StartNew();
            var subscribersFirst = await persister.GetSubscribers(type);
            var firstTime = first.ElapsedMilliseconds;
            var second = Stopwatch.StartNew();
            var subscribersSecond = await persister.GetSubscribers(type);
            var secondTime = second.ElapsedMilliseconds;
            Assert.IsTrue(secondTime * 1000 < firstTime);
            Assert.AreEqual(subscribersFirst.Count(), subscribersSecond.Count());
        }

        [Test]
        public async Task Should_be_cached()
        {
            var type = new MessageType("type1", new Version(0, 0, 0, 0));
            await persister.Subscribe(new Subscriber("address://test-queue", "endpoint"), type, null);
            await persister.GetSubscribers(type);
            VerifyCache(persister.Cache);
        }

        [Test]
        public async Task Subscribe_with_same_type_should_clear_cache()
        {
            var matchingType = new MessageType("matchingType", new Version(0, 0, 0, 0));
            await persister.Subscribe(new Subscriber("address://test-queue", "endpoint"), matchingType, null);
            await persister.GetSubscribers(matchingType);
            await persister.Subscribe(new Subscriber("address://test-queue", "endpoint"), matchingType, null);
            VerifyCache(persister.Cache);
        }

        [Test]
        public async Task Unsubscribe_with_same_type_should_clear_cache()
        {
            var matchingType = new MessageType("matchingType", new Version(0, 0, 0, 0));
            await persister.Subscribe(new Subscriber("address://test-queue", "endpoint"), matchingType, null);
            await persister.GetSubscribers(matchingType);
            await persister.Unsubscribe(new Subscriber("address://test-queue", "endpoint"), matchingType, null);
            VerifyCache(persister.Cache);
        }

        [Test]
        public async Task Unsubscribe_with_part_type_should_partially_clear_cache()
        {
            var version = new Version(0, 0, 0, 0);
            var type1 = new MessageType("type1", version);
            var type2 = new MessageType("type2", version);
            var type3 = new MessageType("type3", version);
            await persister.Subscribe(new Subscriber("address://test-queue", "endpoint"), type1, null);
            await persister.Subscribe(new Subscriber("address://test-queue", "endpoint"), type2, null);
            await persister.Subscribe(new Subscriber("address://test-queue", "endpoint"), type3, null);

            await persister.GetSubscribers(type1);
            await persister.GetSubscribers(type2);
            await persister.GetSubscribers(type3);
            await persister.GetSubscribers(type1, type2);
            await persister.GetSubscribers(type2, type3);
            await persister.GetSubscribers(type1, type3);
            await persister.GetSubscribers(type1, type2, type3);
            await persister.Unsubscribe(new Subscriber("address://test-queue", "endpoint"), type2, null);
            VerifyCache(persister.Cache);
        }

        static void VerifyCache(ConcurrentDictionary<string, AzureSubscriptionStorage.CacheItem> cache, [CallerMemberName] string callerMemberName = null)
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

            Approver.Verify(items, callerMemberName: callerMemberName);
        }
    }
}