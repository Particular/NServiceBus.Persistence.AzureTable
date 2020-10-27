namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Sagas
{
    using System.Net;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using Microsoft.Azure.Cosmos.Table;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    /// <summary>
    /// These tests try to mimic different concurrent scenarios using two persiters trying to access the same saga.
    /// </summary>
    public class When_executing_concurrently
    {
        public When_executing_concurrently()
        {
            connectionString = Testing.Utilities.GetEnvConfiguredConnectionStringForTransport();
            var account = CloudStorageAccount.Parse(connectionString);
            var client = account.CreateCloudTableClient();
            cloudTable = client.GetTableReference(typeof(ConcurrentSagaData).Name);
        }

        [SetUp]
        public async Task SetUp()
        {
            await cloudTable.CreateIfNotExistsAsync().ConfigureAwait(false);
            persister1 = new AzureSagaPersister(new CloudTableClientFromConnectionString(connectionString), true, false);
            persister2 = new AzureSagaPersister(new CloudTableClientFromConnectionString(connectionString), true, false);

            // clear whole table
            var query = cloudTable.CreateQuery<TableEntity>();
            var runningQuery = new TableQuery<DynamicTableEntity>()
            {
                FilterString = query.FilterString,
                SelectColumns = query.SelectColumns
            };
            TableContinuationToken token = null;
            var operationCount = 0;
            do
            {
                runningQuery.TakeCount = query.TakeCount - operationCount;

                var seg = await cloudTable.ExecuteQuerySegmentedAsync(runningQuery, token);
                token = seg.ContinuationToken;
                foreach (var entity in seg)
                {
                    var tableEntity = new DynamicTableEntity(entity.PartitionKey, entity.RowKey)
                    {
                        ETag = "*"
                    };
                    await cloudTable.ExecuteAsync(TableOperation.Delete(tableEntity));
                    operationCount++;
                }

            }
            while (token != null && (query.TakeCount == null || operationCount < query.TakeCount.Value));
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
            try
            {
                await cloudTable.ExecuteAsync(TableOperation.Delete(primary)).ConfigureAwait(false);
            }
            catch (StorageException ex)
            {
                // Horrible logic to check if item has already been deleted or not
                var webException = ex.InnerException as WebException;
                if (webException?.Response != null)
                {
                    var response = (HttpWebResponse) webException.Response;
                    if ((int) response.StatusCode != 404)
                    {
                        // Was not a previously deleted exception, throw again
                        throw;
                    }
                }
            }
        }

        static void AssertSaga(ConcurrentSagaData saga, string value, Guid id)
        {
            Assert.NotNull(saga);
            Assert.AreEqual(id, saga.Id);
            Assert.AreEqual(SagaCorrelationPropertyValue.Value, saga.CorrelationId);
            Assert.AreEqual(value, saga.Value);
        }

        static Task<ConcurrentSagaData> GetByCorrelationProperty(ISagaPersister persister)
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