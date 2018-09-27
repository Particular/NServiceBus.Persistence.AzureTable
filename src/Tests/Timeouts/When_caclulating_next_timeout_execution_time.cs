namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Timeouts
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    [Category("AzureStoragePersistence")]
    public class When_caclulating_next_timeout_execution_time
    {
        [SetUp]
        public Task Perform_storage_cleanup()
        {
            return TestHelper.PerformStorageCleanup();
        }

        [Test]
        public async Task Should_back_off_if_no_timeouts_to_execute()
        {
            var utcNow = DateTime.UtcNow;
            var timeoutPersister = TestHelper.CreateTimeoutPersister(() => utcNow);

            var timeout = TestHelper.GenerateTimeoutWithHeaders();
            await timeoutPersister.Add(timeout, null);

            var timeoutsChunk = await timeoutPersister.GetNextChunk(utcNow);

            Assert.AreEqual(utcNow.AddSeconds(1), timeoutsChunk.NextTimeToQuery, "Should back off by 1 second");

            for (var i = 0; i < 65; i++)
            {
                timeoutsChunk = await timeoutPersister.GetNextChunk(utcNow);
            }

            Assert.AreEqual(utcNow.AddMinutes(1), timeoutsChunk.NextTimeToQuery, "Should back off by 1 minute");

            await TestHelper.PerformStorageCleanup();
        }
    }
}