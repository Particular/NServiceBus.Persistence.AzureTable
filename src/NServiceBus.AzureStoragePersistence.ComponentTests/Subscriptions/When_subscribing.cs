namespace NServiceBus.AzureStoragePersistence.ComponentTests.Subscriptions
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Unicast.Subscriptions;
    using NUnit.Framework;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;
    using Routing;

    [TestFixture]
    [Category("AzureStoragePersistence")]
    public class When_subscribing
    {
        public void Setup()
        {
            SuscriptionTestHelper.PerformStorageCleanup();
        }

        [Test]
        public async void ensure_that_the_subscription_is_persisted()
        {
            var persister = SuscriptionTestHelper.CreateAzureSubscriptionStorage();
            var messageType = new MessageType(typeof(TestMessage));
            var messageTypes = new[] { messageType };

            await persister.Subscribe(new Subscriber("address://test-queue", new EndpointName("endpointName")), messageType, null);

            var subscribers = await persister.GetSubscriberAddressesForMessage(messageTypes, null);

            Assert.That(subscribers.Count(), Is.EqualTo(1));

            var subscription = subscribers.ToArray()[0];
            Assert.That(subscription.TransportAddress, Is.EqualTo("address://test-queue"));
            Assert.That(subscription.Endpoint.ToString(), Is.EqualTo("endpointName"));
        }
    }

    class TestMessage
    {
    }
}