namespace NServiceBus.Persistence.AzureTable.ComponentTests.Subscriptions
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;
    using NUnit.Framework;

    [TestFixture("StorageTable")]
    [TestFixture("CosmosDB")]
    public class When_subscribing
    {
        private string tableApiType;

        public When_subscribing(string tableApiType)
        {
            this.tableApiType = tableApiType;
        }

        [SetUp]
        public Task Setup()
        {
            return SubscriptionTestHelper.PerformStorageCleanup(tableApiType);
        }

        [Test]
        public async Task ensure_that_the_subscription_is_persisted()
        {
            var persister = await SubscriptionTestHelper.CreateAzureSubscriptionStorage(tableApiType);
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
            var persister = await SubscriptionTestHelper.CreateAzureSubscriptionStorage(tableApiType);

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
            var persister = await SubscriptionTestHelper.CreateAzureSubscriptionStorage(tableApiType);

            await persister.Subscribe(new Subscriber("address://test-queue", "endpointName"), new MessageType(typeof(TestMessage)), new ContextBag()).ConfigureAwait(false);
            await persister.Subscribe(new Subscriber("address://test-queue2", "endpointName"), new MessageType(typeof(TestMessagea)), new ContextBag()).ConfigureAwait(false);

            var subscribers = await persister.GetSubscribers(new MessageType(typeof(TestMessage))).ConfigureAwait(false);

            Assert.That(subscribers.Count(), Is.EqualTo(1));

            var subscription = subscribers.ToArray()[0];
            Assert.That(subscription.TransportAddress, Is.EqualTo("address://test-queue"));
            Assert.That(subscription.Endpoint, Is.EqualTo("endpointName"));
        }
    }

}
