namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Timeouts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage.Table;
    using NUnit.Framework;
    using Timeout.Core;

    [TestFixture]
    [Category("AzureStoragePersistence")]
    public class When_querying_timeouts
    {
        [SetUp]
        public void SetUp()
        {
            now = DateTime.Now;
            segments.Clear();
        }

        [Test]
        public async Task Returns_no_timeouts_when_none_exist()
        {
            NextSegmentIs();

            var results = await GetNextChunk();

            CollectionAssert.IsEmpty(results.DueTimeouts);
            Assert.AreEqual(now.Add(TimeoutPersister.DefaultNextQueryDelay), results.NextTimeToQuery);
        }

        [Test]
        public async Task Returns_all_if_due_timeouts_are_within_batch_size()
        {
            var segment1 = NextSegmentIs(DispatchNow());

            var results = await GetNextChunk();

            AssertEqual(segment1, results);
            Assert.AreEqual(now.Add(TimeoutPersister.DefaultNextQueryDelay), results.NextTimeToQuery);
        }

        [Test]
        public async Task Returns_all_if_due_timeouts_are_returned_in_two_segments()
        {
            var segment1 = NextSegmentIs(DispatchNow());
            var segment2 = NextSegmentIs(DispatchNow());

            var results = await GetNextChunk();

            AssertEqual(segment1.Concat(segment2).ToArray(), results);
            Assert.AreEqual(now.Add(TimeoutPersister.DefaultNextQueryDelay), results.NextTimeToQuery);
        }

        [Test]
        public async Task Returns_only_due_timeouts_from_single_segment_scheduling_for_future()
        {
            var segment1 = NextSegmentIs(DispatchNow(), DispatchInFuture());

            var results = await GetNextChunk();

            AssertEqual(segment1.Take(1), results);
            Assert.AreEqual(segment1[1].Time, results.NextTimeToQuery);
        }

        [Test]
        public async Task Schedules_next_within_max_delay_boundary()
        {
            NextSegmentIs(DispatchInFuture(TimeSpan.FromDays(10000)));

            var results = await GetNextChunk();

            Assert.AreEqual(now.Add(TimeoutPersister.MaximumDelay), results.NextTimeToQuery);
        }

        [Test]
        public async Task Scheduling_next_does_not_depend_on_order_of_futures()
        {
            var smallDelay = TimeSpan.FromMinutes(1);
            var bigDelay = TimeSpan.FromMinutes(2);

            // first schedule
            NextSegmentIs(DispatchInFuture(smallDelay));
            NextSegmentIs(DispatchInFuture(bigDelay));
            var results1 = await GetNextChunk();

            // secondschedule
            NextSegmentIs(DispatchInFuture(smallDelay));
            NextSegmentIs(DispatchInFuture(bigDelay));
            var results2 = await GetNextChunk();

            Assert.AreEqual(results1.NextTimeToQuery, results2.NextTimeToQuery);
        }

        [Test]
        public async Task Returns_up_to_batch_size_when_more_than_batch_size()
        {
            var entities = new List<TimeoutDataEntity>();
            for (var i = 0; i < TimeoutPersister.TimeoutChunkBatchSize + 1; i++)
            {
                entities.AddRange(NextSegmentIs(DispatchNow()));
            }

            var results = await GetNextChunk();

            AssertEqual(entities.Take(TimeoutPersister.TimeoutChunkBatchSize), results);
            Assert.AreEqual(now.Add(TimeoutPersister.DefaultNextQueryDelay), results.NextTimeToQuery);
        }

        [Test]
        public async Task Can_return_more_than_batch_size_when_one_segment_adds_to_many()
        {
            var segment1 = NextSegmentIs(DispatchNow());
            var segment2 = NextSegmentIs(Enumerable.Range(1, TimeoutPersister.TimeoutChunkBatchSize + 1).Select(i => DispatchNow()).ToArray());

            var results = await GetNextChunk();

            AssertEqual(segment1.Concat(segment2), results);
            Assert.AreEqual(now.Add(TimeoutPersister.DefaultNextQueryDelay), results.NextTimeToQuery);
        }

        Task<TimeoutsChunk> GetNextChunk()
        {
            var executor = CreateQueryExecutor(segments.ToArray());
            segments.Clear();
            return TimeoutPersister.CalculateNextTimeoutChunk(executor, null, now);
        }

        List<TimeoutDataEntity> NextSegmentIs(params TimeoutDataEntity[] timeoutDataEntities)
        {
            var segment = timeoutDataEntities.ToList();
            segments.Add(segment);
            return segment;
        }

        static void AssertEqual(IEnumerable<TimeoutDataEntity> segment1, TimeoutsChunk results)
        {
            CollectionAssert.AreEquivalent(segment1.Select(s => s.RowKey), results.DueTimeouts.Select(t => t.Id));
        }

        TimeoutDataEntity DispatchNow()
        {
            return new TimeoutDataEntity { Time = now.AddSeconds(-1), RowKey = Guid.NewGuid().ToString() };
        }

        TimeoutDataEntity DispatchInFuture(TimeSpan? delay = null)
        {
            return new TimeoutDataEntity { Time = now.Add(delay ?? DefaultFutureDelay), RowKey = Guid.NewGuid().ToString() };
        }

        static Func<TableQuery<TimeoutDataEntity>, TableContinuationToken, Task<TableQuerySegment<TimeoutDataEntity>>> CreateQueryExecutor(List<TimeoutDataEntity>[] values)
        {
            var counter = 0;
            return (query, token) =>
            {
                if (counter >= values.Length)
                {
                    throw new Exception("Too many calls to provider");
                }

                var results = values[counter];

                counter += 1;

                return Task.FromResult(CreateSegment(results, counter >= values.Length ? null : new TableContinuationToken()));
            };
        }

        static TableQuerySegment<TimeoutDataEntity> CreateSegment(List<TimeoutDataEntity> list, TableContinuationToken token)
        {
            var segment = (TableQuerySegment<TimeoutDataEntity>)Activator.CreateInstance(typeof(TableQuerySegment<TimeoutDataEntity>), BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance | BindingFlags.NonPublic, null, new[] { list }, null, null);
            typeof(TableQuerySegment<TimeoutDataEntity>).GetProperty("ContinuationToken").GetSetMethod(true).Invoke(segment, new[]
            {
                token
            });
            return segment;
        }

        DateTime now;
        List<List<TimeoutDataEntity>> segments = new List<List<TimeoutDataEntity>>();
        static readonly TimeSpan DefaultFutureDelay = TimeSpan.FromMinutes(10);
    }
}