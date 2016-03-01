namespace NServiceBus.AzureStoragePersistence.ComponentTests.Persisters
{
    using System;
    using NServiceBus.Saga;
    using NServiceBus.SagaPersisters.Azure;
    using NUnit.Framework;

    public class When_completing_saga
    {
        [Test]
        public void Should_remove_saga_data()
        {
            var connectionString = AzurePersistenceTests.GetConnectionString();

            var persister = new AzureSagaPersister(connectionString, true);
            var saga = new CompleteSagaData
            {
                Id = Guid.NewGuid(),
                Originator = "Moo",
                OriginalMessageId = "MooId"
            };

            persister.Save(saga);
            var sagaData = persister.Get<CompleteSagaData>(saga.Id);
            Assert.IsNotNull(sagaData);

            persister.Complete(saga);
            sagaData = persister.Get<CompleteSagaData>(saga.Id);
            Assert.IsNull(sagaData);
        }

        [Test]
        public void Should_allow_action_twice_without_throwing_error()
        {
            var connectionString = AzurePersistenceTests.GetConnectionString();

            var persister = new AzureSagaPersister(connectionString, true);
            var saga = new CompleteSagaData
            {
                Id = Guid.NewGuid(),
                Originator = "Moo",
                OriginalMessageId = "MooId"
            };

            persister.Save(saga);
            var sagaData = persister.Get<CompleteSagaData>(saga.Id);
            Assert.IsNotNull(sagaData);

            persister.Complete(saga);
            persister.Complete(saga);

            sagaData = persister.Get<CompleteSagaData>(saga.Id);
            Assert.IsNull(sagaData);
        }

        [Test]
        public void Should_succeed_if_saga_doesnt_exist()
        {
            var connectionString = AzurePersistenceTests.GetConnectionString();

            var persister = new AzureSagaPersister(connectionString, true);
            var saga = new CompleteSagaData
            {
                Id = Guid.NewGuid(),
                Originator = "Moo",
                OriginalMessageId = "MooId"
            };

            persister.Complete(saga);

            var sagaData = persister.Get<CompleteSagaData>(saga.Id);
            Assert.IsNull(sagaData);
        }

    }

    public class CompleteSagaData : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }

}