namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Timeouts
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;
    using NUnit.Framework.Compatibility;
    using Timeout.Core;

    [TestFixture]
    [Category("AzureStoragePersistence")]
    public class When_there_is_lots_of_future_timeouts
    {
        static readonly DateTime WhateverDate = DateTime.Now;

        [SetUp]
        public Task Perform_storage_cleanup()
        {
            return TestHelper.PerformStorageCleanup();
        }

        [Test, Explicit("Used to check what is performance of GetNextChunk with considerable number of timeout rows outside of query criteria")]
        public async Task Should_return_timeouts_to_dispatch_reasonably_fast()
        {
            ServicePointManager.DefaultConnectionLimit = 100;
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.Expect100Continue = false;

            var timeoutPersister = TestHelper.CreateTimeoutPersister();

            var future = DateTime.UtcNow.AddDays(1);

            const int count = 1000;
            var tasks = new Task[count];

            var insertStopWatch = Stopwatch.StartNew();

            for (var i = 0; i < count; i++)
            {
                var t = GenerateTimeout(future);

                tasks[i] = timeoutPersister.Add(t, new ContextBag());
                future = future.AddHours(1);
            }

            await Task.WhenAll(tasks);
            var insertElapsed = insertStopWatch.ElapsedMilliseconds;

            Console.WriteLine($"Insert took {insertElapsed} ms.");

            var outlier = DateTime.UtcNow.AddDays(-1);
            await timeoutPersister.Add(GenerateTimeout(outlier), new ContextBag());

            var stopwatch = Stopwatch.StartNew();
            var peekedTimeout = await timeoutPersister.GetNextChunk(WhateverDate);
            var elapsed = stopwatch.ElapsedMilliseconds;

            Console.WriteLine($"GetNextChunk took {elapsed} ms.");

            Assert.AreEqual(1, peekedTimeout.DueTimeouts.Length);
            Assert.AreEqual(outlier, peekedTimeout.DueTimeouts[0].DueTime);

            await TestHelper.PerformStorageCleanup();
        }

        static TimeoutData GenerateTimeout(DateTime time)
        {
            return new TimeoutData
            {
                Time = time,
                Id = Guid.NewGuid().ToString(),
                Destination = "whatever",
                Headers = new Dictionary<string, string>(),
                OwningTimeoutManager = TestHelper.EndpointName,
                State = new byte[0],
            };
        }
    }
}