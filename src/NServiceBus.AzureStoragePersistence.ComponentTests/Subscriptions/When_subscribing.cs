namespace NServiceBus.AzureStoragePersistence.ComponentTests.Subscriptions
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Unicast.Subscriptions;
    using NUnit.Framework;

    [TestFixture]
    [Category("AzureStoragePersistence")]
    public class When_subscribing
    {
        public void Setup()
        {
            SuscriptionTestHelper.PerformStorageCleanup();
        }

        [Test]
        public void ensure_that_the_subscription_is_persisted()
        {
            var persister = SuscriptionTestHelper.CreateAzureSubscriptionStorage();

            var messageTypes = new List<MessageType>
            {
                new MessageType(typeof(TestMessage))
            };

            persister.Subscribe(new Address("test-queue", "test-machine"), messageTypes);

            var subscribers = persister.GetSubscriberAddressesForMessage(messageTypes);

            Assert.That(subscribers.Count(), Is.EqualTo(1));

            var subscription = subscribers.ToArray()[0];
            Assert.That(subscription.Machine, Is.EqualTo("test-machine"));
            Assert.That(subscription.Queue, Is.EqualTo("test-queue"));
        }
    }

    class TestMessage
    {
    }
}