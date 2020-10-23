namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Persisters
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;

    public class When_updating_saga
    {
        [Test]
        public async Task Should_save_updated_properties()
        {
            var connectionString = Testing.Utilities.GetEnvConfiguredConnectionStringForPersistence();

            var persister = new AzureSagaPersister(new CloudTableClientFromConnectionString(connectionString), true, false);
            var originalProp = Guid.NewGuid().ToString();
            var newProp = Guid.NewGuid().ToString();
            var saga = new UpdateSagaData
            {
                Id = Guid.NewGuid(),
                Originator = "Moo",
                OriginalMessageId = "MooId",
                MyProps = originalProp
            };

            await persister.Save(saga, null, null, new ContextBag());

            var bag = new ContextBag();
            var sagaData = await persister.Get<UpdateSagaData>(saga.Id, null, bag);
            Assert.AreEqual(sagaData.MyProps, saga.MyProps);
            sagaData.MyProps = newProp;
            await persister.Update(sagaData, null, bag);

            var updatedSaga = await persister.Get<UpdateSagaData>(saga.Id, null, new ContextBag());

            Assert.AreEqual(updatedSaga.MyProps, newProp);
        }
    }

    public class UpdateSagaData : IContainSagaData
    {
        public string MyProps { get; set; }
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }
}