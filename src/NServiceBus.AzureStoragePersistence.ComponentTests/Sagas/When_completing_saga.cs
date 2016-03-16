namespace NServiceBus.AzureStoragePersistence.ComponentTests.Persisters
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.SagaPersisters.Azure;
    using NUnit.Framework;

    public class When_completing_saga
    {
        [Test]
        public async Task Should_remove_saga_data()
        {
            var connectionString = AzurePersistenceTests.GetConnectionString();

            var persister = new AzureSagaPersister(connectionString, true);
            var saga = new CompleteSagaData
            {
                Id = Guid.NewGuid(),
                Originator = "Moo",
                OriginalMessageId = "MooId"
            };

            await persister.Save(saga, null, null, null);
            var sagaData = await persister.Get<CompleteSagaData>(saga.Id, null, null);
            Assert.IsNotNull(sagaData);

            await persister.Complete(saga, null, null);
            sagaData = await persister.Get<CompleteSagaData>(saga.Id, null, null);
            Assert.IsNull(sagaData);
        }

        [Test]
        public async Task Should_allow_action_twice_without_throwing_error()
        {
            var connectionString = AzurePersistenceTests.GetConnectionString();

            var persister = new AzureSagaPersister(connectionString, true);
            var saga = new CompleteSagaData
            {
                Id = Guid.NewGuid(),
                Originator = "Moo",
                OriginalMessageId = "MooId"
            };

            await persister.Save(saga, null, null, null);
            var sagaData = await persister.Get<CompleteSagaData>(saga.Id, null, null);
            Assert.IsNotNull(sagaData);

            await persister.Complete(saga, null, null);
            await persister.Complete(saga, null, null);

            sagaData = await persister.Get<CompleteSagaData>(saga.Id, null, null);
            Assert.IsNull(sagaData);
        }

        [Test]
        public async Task Should_succeed_if_saga_doesnt_exist()
        {
            var connectionString = AzurePersistenceTests.GetConnectionString();

            var persister = new AzureSagaPersister(connectionString, true);
            var saga = new CompleteSagaData
            {
                Id = Guid.NewGuid(),
                Originator = "Moo",
                OriginalMessageId = "MooId"
            };

            await persister.Complete(saga, null, null);

            var sagaData = await persister.Get<CompleteSagaData>(saga.Id, null, null);
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