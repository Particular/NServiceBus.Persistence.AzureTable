namespace NServiceBus.Persistence.AzureTable.Tests
{
    using System.Linq;
    using System.Threading.Tasks;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;
    using NUnit.Framework;

    [TestFixture("StorageTable")]
    [TestFixture("CosmosDB")]
    public class When_unsubscribing
    {
        string tableApiType;
        AzureSubscriptionStorage persister;
        SubscriptionTestHelper.Scope scope;

        public When_unsubscribing(string tableApiType) => this.tableApiType = tableApiType;

        [SetUp]
        public async Task Setup()
        {
            scope = await SubscriptionTestHelper.CreateAzureSubscriptionStorage(tableApiType);
            persister = scope.Storage;
        }

        [TearDown]
        public async Task Teardown() => await scope.DisposeAsync();

        [Test]
        public async Task The_subscription_should_be_removed()
        {
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