namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Subscriptions
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;
    using NUnit.Framework;

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
            var messageTypes = new[]
            {
                messageType
            };

            await persister.Subscribe(new Subscriber("address://test-queue", "endpointName"), messageType, null);

            var subscribers = await persister.GetSubscriberAddressesForMessage(messageTypes, null);

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
                await persister.Subscribe(new Subscriber("address://test-queue", "endpointName"), messageType, null);
            }

            var subscribers = await persister.GetSubscriberAddressesForMessage(new[]
            {
                new MessageType(typeof(TestMessage))
            }, null);

            Assert.That(subscribers.Count(), Is.EqualTo(1));

            var subscription = subscribers.ToArray()[0];
            Assert.That(subscription.TransportAddress, Is.EqualTo("address://test-queue"));
            Assert.That(subscription.Endpoint, Is.EqualTo("endpointName"));
        }
    }

    class TestMessage
    {
    }
}