namespace NServiceBus.Persistence.AzureTable.Tests
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
        string tableApiType;
        AzureSubscriptionStorage persister;
        SubscriptionTestHelper.Scope scope;

        public When_subscribing(string tableApiType) => this.tableApiType = tableApiType;

        [SetUp]
        public async Task Setup()
        {
            scope = await SubscriptionTestHelper.CreateAzureSubscriptionStorage(tableApiType);
            persister = scope.Storage;
        }

        [TearDown]
        public async Task Teardown() => await scope.DisposeAsync();

        [Test]
        public async Task Ensure_that_the_subscription_is_persisted()
        {
            var messageType = new MessageType(typeof(TestMessage));
            await persister.Subscribe(new Subscriber("address://test-queue", "endpointName"), messageType, null);

            var subscribers = await persister.GetSubscribers(messageType);

            Assert.That(subscribers.Count(), Is.EqualTo(1));

            var subscription = subscribers.ToArray()[0];
            Assert.That(subscription.TransportAddress, Is.EqualTo("address://test-queue"));
            Assert.That(subscription.Endpoint, Is.EqualTo("endpointName"));
        }

        [Test]
        public async Task Ensure_that_the_subscription_is_version_ignorant()
        {
            var name = typeof(TestMessage).FullName;

            var messageTypes = new[]
            {
                new MessageType(name, new Version(1,2,3)),
                new MessageType(name, new Version(4,2,3)),
            };

            foreach (var messageType in messageTypes)
            {
                await persister.Subscribe(new Subscriber("address://test-queue", "endpointName"), messageType, null);
            }

            var subscribers = await persister.GetSubscribers(new MessageType(typeof(TestMessage)));

            Assert.That(subscribers.Count(), Is.EqualTo(1));

            var subscription = subscribers.ToArray()[0];
            Assert.That(subscription.TransportAddress, Is.EqualTo("address://test-queue"));
            Assert.That(subscription.Endpoint, Is.EqualTo("endpointName"));
        }

        [Test]
        public async Task Ensure_that_the_subscription_selects_proper_message_types()
        {
            await persister.Subscribe(new Subscriber("address://test-queue", "endpointName"), new MessageType(typeof(TestMessage)), new ContextBag());
            await persister.Subscribe(new Subscriber("address://test-queue2", "endpointName"), new MessageType(typeof(TestMessagea)), new ContextBag());

            var subscribers = await persister.GetSubscribers(new MessageType(typeof(TestMessage)));

            Assert.That(subscribers.Count(), Is.EqualTo(1));

            var subscription = subscribers.ToArray()[0];
            Assert.That(subscription.TransportAddress, Is.EqualTo("address://test-queue"));
            Assert.That(subscription.Endpoint, Is.EqualTo("endpointName"));
        }
    }
}