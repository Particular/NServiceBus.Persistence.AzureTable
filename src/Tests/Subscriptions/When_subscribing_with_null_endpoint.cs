namespace NServiceBus.Persistence.AzureTable.Tests
{
    using System.Linq;
    using System.Threading.Tasks;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;
    using NUnit.Framework;

    [TestFixture("StorageTable")]
    [TestFixture("CosmosDB")]
    public class When_subscribing_with_null_endpoint
    {
        string tableApiType;
        AzureSubscriptionStorage persister;
        SubscriptionTestHelper.Scope scope;

        public When_subscribing_with_null_endpoint(string tableApiType)
        {
            this.tableApiType = tableApiType;
        }

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