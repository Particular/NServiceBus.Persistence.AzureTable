namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Timeouts
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;

    [TestFixture]
    [Category("AzureStoragePersistence")]
    public class When_querying_timeouts_via_persister_API
    {
        [SetUp]
        public Task Perform_storage_cleanup()
        {
            return TestHelper.PerformStorageCleanup();
        }

        [Test]
        public async Task Returns_stored_timeout_via_GetNextChunk()
        {
            var now = new DateTime(2017, 1, 1, 1, 1, 1, DateTimeKind.Utc);

            var persister = TestHelper.CreateTimeoutPersister();
            persister.NowGetter = () => now;

            var timeout = TestHelper.GenerateTimeoutWithHeaders();
            timeout.Time = now;

            await persister.Add(timeout, new ContextBag()).ConfigureAwait(false);
            var timeouts = await persister.GetNextChunk(DateTime.MinValue).ConfigureAwait(false);

            Assert.AreEqual(timeout.Id, timeouts.DueTimeouts.Single().Id);
        }
    }
}