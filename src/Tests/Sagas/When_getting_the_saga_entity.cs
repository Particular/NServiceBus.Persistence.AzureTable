namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Persisters
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;

    public class When_getting_the_saga_entity
    {
        [Test]
        public async Task Should_return_null_when_no_saga_data_exists()
        {
            var connectionString = AzurePersistenceTests.GetConnectionString();

            var persister = new AzureSagaPersister(connectionString, false);

            var sagaData = await persister.Get<GetSagaData>(Guid.NewGuid().ToString(), null, null, null);

            Assert.IsNull(sagaData);
        }

        [Test]
        public async Task Should_return_entity()
        {
            var connectionString = AzurePersistenceTests.GetConnectionString();

            var persister = new AzureSagaPersister(connectionString, true);
            var saga = new GetSagaData
            {
                Id = Guid.NewGuid(),
                Originator = "Moo",
                OriginalMessageId = "MooId"
            };

            await persister.Save(saga, null, null, new ContextBag());
            var sagaData = await persister.Get<GetSagaData>(saga.Id, null, new ContextBag());

            Assert.IsNotNull(sagaData);
            Assert.AreEqual(sagaData.OriginalMessageId, saga.OriginalMessageId);
        }
    }

    public class GetSagaData : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }
}