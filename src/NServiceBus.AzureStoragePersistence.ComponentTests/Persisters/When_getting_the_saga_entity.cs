namespace NServiceBus.AzureStoragePersistence.ComponentTests.Persisters
{
    using System;
    using NServiceBus.Saga;
    using NServiceBus.SagaPersisters.Azure;
    using NUnit.Framework;

    public class When_getting_the_saga_entity
    {
        [Test]
        public void Should_return_null_when_no_saga_data_exists()
        {
            var connectionString = AzurePersistenceTests.GetConnectionString();

            var persister = new AzureSagaPersister(connectionString, false);

            var sagaData = persister.Get<GetSagaData>(Guid.NewGuid());

            Assert.IsNull(sagaData);
        }

        [Test]
        public void Should_return_entity()
        {
            var connectionString = AzurePersistenceTests.GetConnectionString();

            var persister = new AzureSagaPersister(connectionString, true);
            var saga = new GetSagaData
            {
                Id = Guid.NewGuid(),
                Originator = "Moo",
                OriginalMessageId = "MooId"
            };

            persister.Save(saga);
            var sagaData = persister.Get<GetSagaData>(saga.Id);

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