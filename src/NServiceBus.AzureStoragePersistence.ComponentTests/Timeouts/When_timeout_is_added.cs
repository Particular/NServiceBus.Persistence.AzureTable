namespace NServiceBus.AzureStoragePersistence.ComponentTests.Timeouts
{
    using NUnit.Framework;

    [TestFixture]
    [Category("AzureStoragePersistence")]
    public class When_timeout_is_added
    {
        [SetUp]
        public void Perform_storage_cleanup()
        {
            TestHelper.PerformStorageCleanup();
        }

        [Test]
        public async void Should_retain_timeout_state()
        {
            var timeoutPersister = TestHelper.CreateTimeoutPersister();
            var timeout = TestHelper.GenerateTimeoutWithHeaders();
            await timeoutPersister.Add(timeout, null);

            var peekedTimeout = await timeoutPersister.Peek(timeout.Id, null);

            Assert.AreEqual(timeout.State, peekedTimeout.State);

            TestHelper.PerformStorageCleanup();
        }
    }
}