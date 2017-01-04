namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Timeouts
{
    using System;
    using System.Collections.Generic;
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
            persister.CurrentDateTimeProvider = () => now;

            var timeout = TestHelper.GenerateTimeoutWithHeaders();
            timeout.Time = now;

            await persister.Add(timeout, new ContextBag()).ConfigureAwait(false);
            var timeouts = await persister.GetNextChunk(DateTime.MinValue).ConfigureAwait(false);

            Assert.AreEqual(timeout.Id, timeouts.DueTimeouts.Single().Id);
        }

        [Test]
        public async Task Returns_all_timeouts_via_GetNextChunk_when_called_sufficient_number_of_times()
        {
            var now = new DateTime(2017, 1, 1, 1, 1, 1, DateTimeKind.Utc);

            var persister = TestHelper.CreateTimeoutPersister();
            persister.CurrentDateTimeProvider = () => now;

            var tailTimeout = TestHelper.GenerateTimeoutWithHeaders();
            tailTimeout.Time = now.AddDays(-1);

            var timeout = TestHelper.GenerateTimeoutWithHeaders();
            timeout.Time = now;

            await persister.Add(tailTimeout, new ContextBag()).ConfigureAwait(false);
            await persister.Add(timeout, new ContextBag()).ConfigureAwait(false);

            var ids = await ReadSpecificNumberOfDistinctTimeouts(persister, 2);

            CollectionAssert.AreEquivalent(new[] { tailTimeout.Id, timeout.Id }, ids);
        }

        static async Task<IEnumerable<string>> ReadSpecificNumberOfDistinctTimeouts(TimeoutPersister persister, int expectedTimeoutCount)
        {
            var ids = new HashSet<string>();
            const int maxLoops = 10;
            for (var i = 0; i < maxLoops; i++)
            {
                var timeoutsChunk = await persister.GetNextChunk(DateTime.MinValue).ConfigureAwait(false);
                if (timeoutsChunk.DueTimeouts.Length > 0)
                {
                    foreach (var t in timeoutsChunk.DueTimeouts)
                    {
                        await persister.TryRemove(t.Id, new ContextBag()).ConfigureAwait(false);
                        ids.Add(t.Id);
                    }
                }

                if (ids.Count == expectedTimeoutCount)
                {
                    return ids;
                }
            }

            return ids;
        }
    }
}