namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Sagas
{
    using System;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
    using Extensibility;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using NUnit.Framework;

    public class When_storing_saga_with_non_primitive_values
    {
        [Test]
        public async Task Should_persist_json_serializable_value()
        {
            var connectionString = Testing.Utillities.GetEnvConfiguredConnectionStringForTransport();

            var persister = new AzureSagaPersister(connectionString, true);
            var array = new[] { 1, 2, 3, 4 };
            double? nullableDouble = 4.5;

            var saga = new NonPrimitiveSerializableSagaData
            {
                Id = Guid.NewGuid(),
                Originator = "Moo",
                OriginalMessageId = "MooId",
                NonPrimitiveValue = array,
                NullableDouble = nullableDouble
            };

            await persister.Save(saga, null, null, new ContextBag());

            var sagaData = await persister.Get<NonPrimitiveSerializableSagaData>(saga.Id, null, new ContextBag());

            // assert values
            CollectionAssert.AreEqual(array, sagaData.NonPrimitiveValue);
            Assert.AreEqual(nullableDouble, sagaData.NullableDouble);

            // assert structure
            var entity = await GetEntity(saga.Id);
            var nullableDoubleProp = entity[nameof(NonPrimitiveSerializableSagaData.NullableDouble)];
            Assert.AreEqual(EdmType.Double, nullableDoubleProp.PropertyType);
            Assert.AreEqual(nullableDouble, nullableDoubleProp.DoubleValue);
        }

        static async Task<DictionaryTableEntity> GetEntity(Guid sagaId)
        {
            var tableName = typeof(NonPrimitiveSerializableSagaData).Name;
            var account = CloudStorageAccount.Parse(Testing.Utillities.GetEnvConfiguredConnectionStringForTransport());
            var table = account.CreateCloudTableClient().GetTableReference(tableName);

            var query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sagaId.ToString()));

            try
            {
                var tableEntity = (await table.ExecuteQueryAsync(query).ConfigureAwait(false)).SafeFirstOrDefault();
                return tableEntity;
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    return null;
                }

                throw;
            }
        }

        class NonPrimitiveSerializableSagaData : IContainSagaData
        {
            public Guid Id { get; set; }
            public string Originator { get; set; }
            public string OriginalMessageId { get; set; }
            public int[] NonPrimitiveValue { get; set; }
            public double? NullableDouble { get; set; }
        }

        [Test]
        public void Should_fail_with_json_non_serializable_value()
        {
            var connectionString = Testing.Utillities.GetEnvConfiguredConnectionStringForTransport();

            var persister = new AzureSagaPersister(connectionString, true);

            var saga = new NonSerializableSagaData
            {
                Id = Guid.NewGuid(),
                Originator = "Moo",
                OriginalMessageId = "MooId",
                NonserializableValue = new SomethingComplex { Disposable = new StringWriter() }
            };

            Assert.ThrowsAsync<NotSupportedException>(() => persister.Save(saga, null, null, new ContextBag()));
        }

        class NonSerializableSagaData : IContainSagaData
        {
            public Guid Id { get; set; }
            public string Originator { get; set; }
            public string OriginalMessageId { get; set; }
            public SomethingComplex NonserializableValue { get; set; }
        }

        class SomethingComplex
        {
            public IDisposable Disposable { get; set; }
        }

    }
}