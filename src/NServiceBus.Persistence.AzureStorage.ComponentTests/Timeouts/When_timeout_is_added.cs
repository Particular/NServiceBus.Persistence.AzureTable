namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Timeouts
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    [Category("AzureStoragePersistence")]
    public class When_timeout_is_added
    {
        [SetUp]
        public Task Perform_storage_cleanup()
        {
            return TestHelper.PerformStorageCleanup();
        }

        [Test]
        public async Task Should_retain_timeout_state()
        {
            var timeoutPersister = TestHelper.CreateTimeoutPersister();
            var timeout = TestHelper.GenerateTimeoutWithHeaders();
            await timeoutPersister.Add(timeout, null);

            var peekedTimeout = await timeoutPersister.Peek(timeout.Id, null);

            Assert.AreEqual(timeout.State, peekedTimeout.State);

            await TestHelper.PerformStorageCleanup();
        }
    }
}