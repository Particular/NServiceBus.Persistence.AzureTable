namespace NServiceBus.AzureStoragePersistence.ComponentTests.Subscriptions
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Unicast.Subscriptions;
    using NUnit.Framework;

    [TestFixture]
    [Category("AzureStoragePersistence")]
    public class When_unsubscribing
    {
        public void Setup()
        {
            SuscriptionTestHelper.PerformStorageCleanup();
        }

        [Test]
        public void the_subscription_should_be_removed()
        {
            var persister = SuscriptionTestHelper.CreateAzureSubscriptionStorage();

            var messageTypes = new List<MessageType>
            {
                new MessageType(typeof(TestMessage))
            };

            var address = new Address("test-queue", "test-machine");

            persister.Subscribe(address, messageTypes);

            var subscribers = persister.GetSubscriberAddressesForMessage(messageTypes);

            Assert.That(subscribers.Count(), Is.EqualTo(1));

            var subscription = subscribers.ToArray()[0];
            Assert.That(subscription.Machine, Is.EqualTo(address.Machine));
            Assert.That(subscription.Queue, Is.EqualTo(address.Queue));

            persister.Unsubscribe(address,messageTypes);

            var postUnsubscribe = persister.GetSubscriberAddressesForMessage(messageTypes);

            Assert.That(postUnsubscribe.Count(), Is.EqualTo(0));
        } 
    }
}