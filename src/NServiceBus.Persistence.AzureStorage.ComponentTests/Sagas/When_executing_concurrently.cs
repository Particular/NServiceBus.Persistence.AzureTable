namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Sagas
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    /// <summary>
    /// These tests try to mimic different concurrent scenarios using two persiters trying to access the same saga.
    /// </summary>
    public class When_executing_concurrently
    {
        public When_executing_concurrently()
        {
            connectionString = AzurePersistenceTests.GetConnectionString();
            var account = CloudStorageAccount.Parse(connectionString);
            var client = account.CreateCloudTableClient();
            cloudTable = client.GetTableReference(typeof(ConcurrentSagaData).Name);
        }

        [SetUp]
        public async Task SetUp()
        {
            await cloudTable.CreateIfNotExistsAsync().ConfigureAwait(false);
            persister1 = new AzureSagaPersister(connectionString, true);
            persister2 = new AzureSagaPersister(connectionString, true);

            // clear whole table
            var entities = await cloudTable.ExecuteQueryAsync(new TableQuery<TableEntity>()).ConfigureAwait(false);
            foreach (var te in entities)
            {
                await cloudTable.DeleteIgnoringNotFound(te).ConfigureAwait(false);
            }
        }

        [Test(Description = "Failed removal of the secondary index")]
        public async Task Should_enable_inserting_when_only_secondary_exists()
        {
            const string v = "1";
            await Save(persister1, v, Id1).ConfigureAwait(false);

            // get by property just to load to cache
            await GetByProperty(persister2).ConfigureAwait(false);

            var entities = await cloudTable.ExecuteQueryAsync(new TableQuery<TableEntity>()).ConfigureAwait(false);
            Guid guid;
            var primary = entities.Single(te => Guid.TryParse(te.PartitionKey, out guid));
            await cloudTable.DeleteIgnoringNotFound(primary);

            // only secondary exists now, ensure it's null
            var byProperty = await GetByProperty(persister2).ConfigureAwait(false);
            Assert.IsNull(byProperty);

            const string v2 = "2";

            // save a new version
            await Save(persister1, v2, Id2).ConfigureAwait(false);

            byProperty = await GetByProperty(persister2).ConfigureAwait(false);
            AssertSaga(byProperty, v2, Id2);
        }

        [Test]
        public Task Should_enable_insert_saga_again_through_same_persister()
        {
            return Should_enable_insert_saga_again(persister1);
        }

        [Test]
        public Task Should_enable_insert_saga_again_through_another_persister()
        {
            return Should_enable_insert_saga_again(persister2);
        }

        async Task Should_enable_insert_saga_again(ISagaPersister p)
        {
            const string v = "1";

            await Save(persister1, v, Id1).ConfigureAwait(false);

            var saga1 = await Get(persister1, Id1).ConfigureAwait(false);
            var saga2 = await Get(persister2, Id1).ConfigureAwait(false);
            var saga1ByProperty = await GetByProperty(persister1).ConfigureAwait(false);
            var saga2ByProperty = await GetByProperty(persister2).ConfigureAwait(false);

            AssertSaga(saga1, v, Id1);
            AssertSaga(saga2, v, Id1);
            AssertSaga(saga1ByProperty, v, Id1);
            AssertSaga(saga2ByProperty, v, Id1);

            await Complete(saga1, persister1).ConfigureAwait(false);

            saga1 = await Get(persister1, Id1).ConfigureAwait(false);
            saga2 = await Get(persister2, Id1).ConfigureAwait(false);
            saga1ByProperty = await GetByProperty(persister1).ConfigureAwait(false);
            saga2ByProperty = await GetByProperty(persister2).ConfigureAwait(false);

            Assert.IsNull(saga1);
            Assert.IsNull(saga2);
            Assert.IsNull(saga1ByProperty);
            Assert.IsNull(saga2ByProperty);

            const string v2 = "2";
            await Save(p, v2, Id2).ConfigureAwait(false);

            saga1 = await Get(persister1, Id2).ConfigureAwait(false);
            saga2 = await Get(persister2, Id2).ConfigureAwait(false);
            saga1ByProperty = await GetByProperty(persister1).ConfigureAwait(false);
            saga2ByProperty = await GetByProperty(persister2).ConfigureAwait(false);

            AssertSaga(saga1, v2, Id2);
            AssertSaga(saga2, v2, Id2);
            AssertSaga(saga1ByProperty, v2, Id2);
            AssertSaga(saga2ByProperty, v2, Id2);
        }

        static Task Complete(IContainSagaData saga, ISagaPersister persister)
        {
            return persister.Complete(saga, null, null);
        }

        static void AssertSaga(ConcurrentSagaData saga, string value, Guid id)
        {
            Assert.NotNull(saga);
            Assert.AreEqual(id, saga.Id);
            Assert.AreEqual(SagaCorrelationPropertyValue.Value, saga.CorrelationId);
            Assert.AreEqual(value, saga.Value);
        }

        static Task<ConcurrentSagaData> Get(ISagaPersister persister, Guid id)
        {
            return persister.Get<ConcurrentSagaData>(id, null, null);
        }

        static Task<ConcurrentSagaData> GetByProperty(ISagaPersister persister)
        {
            return persister.Get<ConcurrentSagaData>(SagaCorrelationPropertyValue.Name, SagaCorrelationPropertyValue.Value, null, null);
        }

        static Task Save(ISagaPersister persister, string value, Guid id)
        {
            return persister.Save(new ConcurrentSagaData
            {
                Id = id,
                CorrelationId = CorrelationIdValue,
                Value = value
            }, SagaCorrelationPropertyValue, null, null);
        }

        readonly CloudTable cloudTable;
        readonly string connectionString;

        AzureSagaPersister persister1;
        AzureSagaPersister persister2;
        const string CorrelationIdValue = "DB0F4000-5B9C-4ADE-9AB0-04305A5CABBD";

        static readonly Guid Id1 = new Guid("7FCF55F6-4AEB-40C7-86B9-2AB535664381");
        static readonly Guid Id2 = new Guid("7FCF55F6-4AEB-40C7-86B9-2AB535664381");
        static readonly SagaCorrelationProperty SagaCorrelationPropertyValue = new SagaCorrelationProperty("CorrelationId", CorrelationIdValue);

        class ConcurrentSagaData : ContainSagaData
        {
            public string CorrelationId { get; set; }
            public string Value { get; set; }
        }
    }
}