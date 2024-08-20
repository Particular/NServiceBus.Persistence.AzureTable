namespace NServiceBus.Persistence.AzureTable.Tests
{
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    [TestFixture("StorageTable")]
    [TestFixture("CosmosDB")]
    public class When_updating_subscription
    {
        string tableApiType;
        AzureSubscriptionStorage persister;
        SubscriptionTestHelper.Scope scope;

        public When_updating_subscription(string tableApiType) => this.tableApiType = tableApiType;

        [SetUp]
        public async Task Setup()
        {
            scope = await SubscriptionTestHelper.CreateAzureSubscriptionStorage(tableApiType);
            persister = scope.Storage;
        }

        [TearDown]
        public async Task Teardown() => await scope.DisposeAsync();

        [Test]
        public async Task New_subscription_should_overwrite_existing()
        {
            var messageType = new MessageType(typeof(TestMessage));

            await persister.Subscribe(new Subscriber("address://test-queue", null), messageType, null);
            await persister.Subscribe(new Subscriber("address://test-queue", "1"), messageType, null);
            await persister.Subscribe(new Subscriber("address://test-queue", "2"), messageType, null);

            var subscribers = (await persister.GetSubscriberAddressesForMessage(new[]
            {
                messageType
            }, null)).ToArray();

            Assert.That(subscribers.Length, Is.EqualTo(1));
            Assert.That(subscribers[0].TransportAddress, Is.EqualTo("address://test-queue"));
            Assert.That(subscribers[0].Endpoint, Is.EqualTo("2"));
        }
    }
}