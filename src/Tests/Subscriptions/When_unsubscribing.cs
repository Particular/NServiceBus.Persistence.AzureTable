namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Subscriptions
{
    using System.Linq;
    using System.Threading.Tasks;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;
    using NUnit.Framework;

    [TestFixture]
    [Category("AzureStoragePersistence")]
    public class When_unsubscribing
    {
        [SetUp]
        public Task Setup()
        {
            return SubscriptionTestHelper.PerformStorageCleanup();
        }

        [Test]
        public async Task The_subscription_should_be_removed()
        {
            var persister = await SubscriptionTestHelper.CreateAzureSubscriptionStorage();
            var messageType = new MessageType(typeof(TestMessage));
            var messageTypes = new[]
            {
                messageType
            };

            var subscriber = new Subscriber("address://test-queue", "endpointName");
            await persister.Subscribe(subscriber, messageType, null);

            var subscribers = (await persister.GetSubscriberAddressesForMessage(messageTypes, null)).ToList();

            Assert.That(subscribers.Count(), Is.EqualTo(1));

            var subscription = subscribers.ToArray()[0];
            Assert.That(subscription.Endpoint, Is.EqualTo(subscriber.Endpoint));
            Assert.That(subscription.TransportAddress, Is.EqualTo(subscriber.TransportAddress));

            await persister.Unsubscribe(subscriber, messageType, null);
            var postUnsubscribe = await persister.GetSubscriberAddressesForMessage(messageTypes, null);

            Assert.That(postUnsubscribe.Count(), Is.EqualTo(0));
        }
    }
}