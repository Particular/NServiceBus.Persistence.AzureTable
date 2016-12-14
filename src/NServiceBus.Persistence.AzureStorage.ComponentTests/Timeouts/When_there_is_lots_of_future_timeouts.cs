namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Timeouts
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;
    using Timeout.Core;

    [TestFixture]
    [Category("AzureStoragePersistence")]
    public class When_there_is_lots_of_future_timeouts
    {
        static readonly DateTime WhateverDate = DateTime.Now;

        [SetUp]
        public void Perform_storage_cleanup()
        {
            TestHelper.PerformStorageCleanup();
        }

        [Test]
        public async Task Should_return_timeouts_to_dispatch_reasonably_fast()
        {
            var timeoutPersister = TestHelper.CreateTimeoutPersister();
            
            var future = DateTime.UtcNow.AddDays(1);

            const int count = 1;
            var tasks = new Task[count];

            for (var i = 0; i < count; i++)
            {
                var t = GenerateTimeout(future);

                tasks[i] = timeoutPersister.Add(t, new ContextBag());
                future = future.AddHours(1);
            }

            await Task.WhenAll(tasks);

            var outlier = DateTime.UtcNow.AddDays(-1);
            await timeoutPersister.Add(GenerateTimeout(outlier), new ContextBag());

            var peekedTimeout = await timeoutPersister.GetNextChunk(WhateverDate);

            Assert.AreEqual(1, peekedTimeout.DueTimeouts.Length);
            Assert.AreEqual(outlier, peekedTimeout.DueTimeouts[0].DueTime);

            TestHelper.PerformStorageCleanup();
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