namespace NServiceBus.AzureStoragePersistence.SagaDeduplicator.Tests.Index
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using NServiceBus.AzureStoragePersistence.SagaDeduplicator.Index;
    using NUnit.Framework;

    public class SagaIndexerTests
    {
        readonly CloudTableClient cloudTableClient;
        private CloudTable cloudTable;
        private Stopwatch sw;
        private int previous;

        public SagaIndexerTests()
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureStorageQueueTransport.ConnectionString");
            var account = CloudStorageAccount.Parse(connectionString);
            cloudTableClient = account.CreateCloudTableClient();
        }

        [SetUp]
        public void SetUp()
        {
            cloudTable = cloudTableClient.GetTableReference("SagaDeduplicator" + Guid.NewGuid().ToString().Replace("-",""));
            cloudTable.CreateIfNotExists();

            sw = Stopwatch.StartNew();
            previous = ServicePointManager.DefaultConnectionLimit;
            ServicePointManager.DefaultConnectionLimit = 100;
        }

        [TearDown]
        public void TearDown()
        {
            Console.WriteLine($"Elapsed time: {sw.Elapsed}");
            ServicePointManager.DefaultConnectionLimit = previous;
            cloudTable.DeleteIfExistsAsync();
        }

        [TestCase(1024)]
        public void When_duplicates_found_Should_raise_an_event_for_every_duplicate_set_found(int testSize)
        {
            const int NumberOfCollisionsForEach = 2;
            var modulo = testSize / NumberOfCollisionsForEach;

            var toCreate = Enumerable.Range(0, testSize).Select(i => Tuple.Create(i, Guid.NewGuid())).ToArray();

            const int concurrency = 100;
            var semaphore = new SemaphoreSlim(concurrency);

            foreach (var t in toCreate)
            {
                semaphore.Wait();
                cloudTable.ExecuteAsync(TableOperation.Insert(CreateSagaState(t, modulo)))
                    .ContinueWith(task =>
                    {
                        if (task.Exception != null)
                        {
                            Console.WriteLine($"Exception occured {task.Exception}");
                        }
                        semaphore.Release();
                    });
            }

            for (var i = 0; i < concurrency; i++)
            {
                semaphore.Wait();
            }

            var comparer = EqualityComparers.GetValueComparer(EdmType.Int64);
            var indexer = new SagaIndexer(cloudTable, "CorrelatingId", o => (ulong)(long)o, comparer);
            var results = new List<Tuple<Guid, Guid[]>>();

            indexer.SearchForDuplicates((o, guids) => results.Add(Tuple.Create(o, guids.ToArray())));

            var dict = results
                .GroupBy(t => t.Item1, t => t.Item2, comparer)
                .ToDictionary(g => g.Key, g => g.SelectMany(ids => ids).Distinct().ToArray(), comparer);

            Assert.AreEqual(modulo, dict.Count);
            foreach (var kvp in dict)
            {
                Assert.AreEqual(2, kvp.Value.Length);
            }
        }

        private static SagaState CreateSagaState(Tuple<int, Guid> t, int modulo)
        {
            return new SagaState
            {
                PartitionKey = t.Item2.ToString(),
                RowKey = "",
                CorrelatingId = t.Item1%modulo,
                Name = "name"+t.Item1,
            };
        }

        private class SagaState : TableEntity
        {
            public long CorrelatingId { get; set; }
            public string Name { get; set; }
        }
    }
}