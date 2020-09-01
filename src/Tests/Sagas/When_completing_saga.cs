namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Persisters
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;

    public class When_completing_saga
    {
        [Test]
        public async Task Should_remove_saga_data()
        {
            var connectionString = Testing.Utillities.GetEnvConfiguredConnectionStringForPersistence();

            var persister = new AzureSagaPersister(connectionString, true);
            var saga = new CompleteSagaData
            {
                Id = Guid.NewGuid(),
                Originator = "Moo",
                OriginalMessageId = "MooId"
            };

            var bag = new ContextBag();
            await persister.Save(saga, null, null, bag);
            var sagaData = await persister.Get<CompleteSagaData>(saga.Id, null, bag);
            Assert.IsNotNull(sagaData);

            await persister.Complete(saga, null, bag);
            sagaData = await persister.Get<CompleteSagaData>(saga.Id, null, new ContextBag());
            Assert.IsNull(sagaData);
        }

        [Test]
        public async Task Should_allow_action_twice_without_throwing_error()
        {
            var connectionString = Testing.Utillities.GetEnvConfiguredConnectionStringForPersistence();

            var persister = new AzureSagaPersister(connectionString, true);
            var sagaData = new CompleteSagaData
            {
                Id = Guid.NewGuid(),
                Originator = "Moo",
                OriginalMessageId = "MooId"
            };

            var bag = new ContextBag();
            await persister.Save(sagaData, null, null, bag);
            Assert.IsNotNull(sagaData);

            await persister.Complete(sagaData, null, bag);
            await persister.Complete(sagaData, null, bag);

            sagaData = await persister.Get<CompleteSagaData>(sagaData.Id, null, null);
            Assert.IsNull(sagaData);
        }

        [Test]
        public async Task Should_succeed_if_saga_doesnt_exist()
        {
            var connectionString = Testing.Utillities.GetEnvConfiguredConnectionStringForPersistence();

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

    public class TestSaga : Saga<CompleteSagaData>, IAmStartedByMessages<CompleteSagaData>
    {
        public Task Handle(CompleteSagaData message, IMessageHandlerContext context)
        {
            return Task.CompletedTask;
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<CompleteSagaData> mapper)
        {
        }
    }

    public class CompleteSagaData : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }
}