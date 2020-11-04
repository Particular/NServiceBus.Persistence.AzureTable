namespace NServiceBus.Persistence.AzureTable.ComponentTests.Subscriptions
{
    using System.Linq;
    using System.Threading.Tasks;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;
    using NUnit.Framework;

    [TestFixture]
    public class When_subscribing_with_null_endpoint
    {
        [SetUp]
        public Task Setup()
        {
           return SubscriptionTestHelper.PerformStorageCleanup();
        }

        [Test]
        public async Task ensure_that_the_subscription_is_persisted()
        {
            var persister = await SubscriptionTestHelper.CreateAzureSubscriptionStorage();
            var messageType = new MessageType(typeof(TestMessage));
            var messageTypes = new[]
            {
                messageType
            };

            await persister.Subscribe(new Subscriber("address://test-queue", null), messageType, null);

            var subscribers = await persister.GetSubscriberAddressesForMessage(messageTypes, null);

            Assert.That(subscribers.Count(), Is.EqualTo(1));

            var subscription = subscribers.ToArray()[0];
            Assert.That(subscription.TransportAddress, Is.EqualTo("address://test-queue"));
            Assert.That(subscription.Endpoint, Is.Null);
        }
    }
}