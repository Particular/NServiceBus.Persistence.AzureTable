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
        private string tableApiType;

        public When_updating_subscription(string tableApiType)
        {
            this.tableApiType = tableApiType;
        }

        [SetUp]
        public Task Setup()
        {
           return SubscriptionTestHelper.PerformStorageCleanup(tableApiType);
        }

        [Test]
        public async Task New_subscription_should_overwrite_existing()
        {
            var persister = await SubscriptionTestHelper.CreateAzureSubscriptionStorage(tableApiType);
            var messageType = new MessageType(typeof(TestMessage));

            await persister.Subscribe(new Subscriber("address://test-queue", null), messageType, null);
            await persister.Subscribe(new Subscriber("address://test-queue", "1"), messageType, null);
            await persister.Subscribe(new Subscriber("address://test-queue", "2"), messageType, null);

            var subscribers = (await persister.GetSubscriberAddressesForMessage(new[]
            {
                messageType
            }, null)).ToArray();

            Assert.AreEqual(1, subscribers.Length);
            Assert.AreEqual("address://test-queue", subscribers[0].TransportAddress);
            Assert.AreEqual("2", subscribers[0].Endpoint);
        }
    }
}