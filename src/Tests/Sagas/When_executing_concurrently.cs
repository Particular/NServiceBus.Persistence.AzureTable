namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Sagas
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
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
            connectionString = Testing.Utillities.GetEnvConfiguredConnectionStringForTransport();
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

        [Test(Description = "The test covering a scenario, when a secondary index wasn't deleted properly")]
        public async Task Should_not_find_saga_when_primary_is_removed_but_secondary_exists()
        {
            const string v = "1";
            await Save(persister1, v, Id1).ConfigureAwait(false);

            // get by property just to load to cache
            await GetByCorrelationProperty(persister2).ConfigureAwait(false);

            await DeletePrimary(Id1).ConfigureAwait(false);

            // only secondary exists now, ensure it's null
            var byProperty = await GetByCorrelationProperty(persister2).ConfigureAwait(false);
            Assert.IsNull(byProperty);
        }

        [Test(Description = "The test covering a scenario, when a secondary index wasn't deleted properly")]
        public async Task Should_enable_saving_another_saga_with_same_correlation_id_as_completed()
        {
            const string v = "1";
            await Save(persister1, v, Id1).ConfigureAwait(false);

            // get by property just to load to cache
            await GetByCorrelationProperty(persister2).ConfigureAwait(false);

            await DeletePrimary(Id1).ConfigureAwait(false);

            const string v2 = "2";

            // save a new saga with the same correlation id
            await Save(persister1, v2, Id2).ConfigureAwait(false);

            var saga = await GetByCorrelationProperty(persister2).ConfigureAwait(false);
            AssertSaga(saga, v2, Id2);
        }

        async Task DeletePrimary(Guid sagaId)
        {
            var entities = await cloudTable.ExecuteQueryAsync(new TableQuery<TableEntity>()).ConfigureAwait(false);
            var primary = entities.Single(te => Guid.TryParse(te.PartitionKey, out var guid) && guid == sagaId);
            await cloudTable.DeleteIgnoringNotFound(primary);
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
            var saga1ByProperty = await GetByCorrelationProperty(persister1).ConfigureAwait(false);
            var saga2ByProperty = await GetByCorrelationProperty(persister2).ConfigureAwait(false);

            AssertSaga(saga1, v, Id1);
            AssertSaga(saga2, v, Id1);
            AssertSaga(saga1ByProperty, v, Id1);
            AssertSaga(saga2ByProperty, v, Id1);

            await Complete(saga1, persister1).ConfigureAwait(false);

            saga1 = await Get(persister1, Id1).ConfigureAwait(false);
            saga2 = await Get(persister2, Id1).ConfigureAwait(false);
            saga1ByProperty = await GetByCorrelationProperty(persister1).ConfigureAwait(false);
            saga2ByProperty = await GetByCorrelationProperty(persister2).ConfigureAwait(false);

            Assert.IsNull(saga1);
            Assert.IsNull(saga2);
            Assert.IsNull(saga1ByProperty);
            Assert.IsNull(saga2ByProperty);

            const string v2 = "2";
            await Save(p, v2, Id2).ConfigureAwait(false);

            saga1 = await Get(persister1, Id2).ConfigureAwait(false);
            saga2 = await Get(persister2, Id2).ConfigureAwait(false);
            saga1ByProperty = await GetByCorrelationProperty(persister1).ConfigureAwait(false);
            saga2ByProperty = await GetByCorrelationProperty(persister2).ConfigureAwait(false);

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
            return persister.Get<ConcurrentSagaData>(id, null, new ContextBag());
        }

        static Task<ConcurrentSagaData> GetByCorrelationProperty(ISagaPersister persister)
        {
            return persister.Get<ConcurrentSagaData>(SagaCorrelationPropertyValue.Name, SagaCorrelationPropertyValue.Value, null, new ContextBag());
        }

        static Task Save(ISagaPersister persister, string value, Guid id)
        {
            return persister.Save(new ConcurrentSagaData
            {
                Id = id,
                CorrelationId = CorrelationIdValue,
                Value = value
            }, SagaCorrelationPropertyValue, null, new ContextBag());
        }

        readonly CloudTable cloudTable;
        readonly string connectionString;

        AzureSagaPersister persister1;
        AzureSagaPersister persister2;
        const string CorrelationIdValue = "DB0F4000-5B9C-4ADE-9AB0-04305A5CABBD";

        static readonly Guid Id1 = new Guid("7FCF55F6-4AEB-40C7-86B9-2AB535664381");
        static readonly Guid Id2 = new Guid("2C739583-0077-4482-BA6E-E569DD129BD6");
        static readonly SagaCorrelationProperty SagaCorrelationPropertyValue = new SagaCorrelationProperty("CorrelationId", CorrelationIdValue);

        class ConcurrentSagaData : ContainSagaData
        {
            public string CorrelationId { get; set; }
            public string Value { get; set; }
        }
    }
}