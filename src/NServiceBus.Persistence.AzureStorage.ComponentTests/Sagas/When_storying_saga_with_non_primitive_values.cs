namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Sagas
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;

    public class When_storying_saga_with_non_primitive_values
    {
        [Test]
        public async Task Should_persist_json_serializable_value()
        {
            var connectionString = AzurePersistenceTests.GetConnectionString();

            var persister = new AzureSagaPersister(connectionString, true);
            var value = new[] { 1, 2, 3, 4 };

            var saga = new NonPrimitiveSerializableSagaData
            {
                Id = Guid.NewGuid(),
                Originator = "Moo",
                OriginalMessageId = "MooId",
                NonPrimitiveValue = value
            };

            await persister.Save(saga, null, null, new ContextBag());

            var bag = new ContextBag();
            var sagaData = await persister.Get<NonPrimitiveSerializableSagaData>(saga.Id, null, bag);
            
            CollectionAssert.AreEqual(value, sagaData.NonPrimitiveValue);
        }

        class NonPrimitiveSerializableSagaData : IContainSagaData
        {
            public Guid Id { get; set; }
            public string Originator { get; set; }
            public string OriginalMessageId { get; set; }
            public int[] NonPrimitiveValue { get; set; }
        }

        [Test]
        public void Should_fail_with_json_non_serializable_value()
        {
            var connectionString = AzurePersistenceTests.GetConnectionString();

            var persister = new AzureSagaPersister(connectionString, true);

            var saga = new NonSerializableSagaData
            {
                Id = Guid.NewGuid(),
                Originator = "Moo",
                OriginalMessageId = "MooId",
                NonserializableValue = new SomethingComplex { Disposable = new StringWriter()}
            };

            Assert.ThrowsAsync<NotSupportedException>(()=>persister.Save(saga, null, null, new ContextBag()));
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