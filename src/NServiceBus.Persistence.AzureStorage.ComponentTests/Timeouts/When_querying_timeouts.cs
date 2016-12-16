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
        [Test]
        public async Task Returns_no_timeouts_when_none_exist()
        {
            var now = DateTime.Now;
            var executor = CreateQueryExecutor(new List<TimeoutDataEntity>());
            var results = await TimeoutPersister.CalculateNextTimeoutChunk(executor, null, now);

            CollectionAssert.IsEmpty(results.DueTimeouts);
            Assert.AreEqual(now.Add(TimeoutPersister.DefaultNextQueryDelay), results.NextTimeToQuery);
        }

        [Test]
        public async Task Returns_all_if_all_are_current_and_within_batch_size()
        {
            var now = DateTime.Now;
            var segment1 = new List<TimeoutDataEntity> { CreateCurrentTimeout(now) };
            var executor = CreateQueryExecutor(segment1);

            var results = await TimeoutPersister.CalculateNextTimeoutChunk(executor, null, now);

            AssertEqual(segment1, results);
            Assert.AreEqual(now.Add(TimeoutPersister.DefaultNextQueryDelay), results.NextTimeToQuery);
        }

        static void AssertEqual(IEnumerable<TimeoutDataEntity> segment1, TimeoutsChunk results)
        {
            CollectionAssert.AreEquivalent(segment1.Select(s => s.RowKey), results.DueTimeouts.Select(t => t.Id));
        }

        static TimeoutDataEntity CreateCurrentTimeout(DateTime now)
        {
            return new TimeoutDataEntity { Time = now.AddSeconds(-1), RowKey = Guid.NewGuid().ToString()};
        }

        static Func<TableQuery<TimeoutDataEntity>, TableContinuationToken, Task<TableQuerySegment<TimeoutDataEntity>>> CreateQueryExecutor(params List<TimeoutDataEntity>[] values)
        {
            var counter = 0;
            return (query, token) =>
            {
                if (counter >= values.Length)
                {
                    throw new Exception("Too many calls to provider");
                }

                var results = values[counter++];
                counter += 1;
                return Task.FromResult(CreateSegment(results, null));
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
    }
}