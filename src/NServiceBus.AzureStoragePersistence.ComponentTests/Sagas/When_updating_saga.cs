namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Persisters
{
    using System;
    using System.Threading.Tasks;
    using SagaPersisters.AzureStoragePersistence;
    using NUnit.Framework;

    public class When_updating_saga
    {
        [Test]
        public async Task Should_save_updated_properties()
        {
            var connectionString = AzurePersistenceTests.GetConnectionString();

            var persister = new AzureSagaPersister(connectionString, true);
            var originalProp = Guid.NewGuid().ToString();
            var newProp = Guid.NewGuid().ToString();
            var saga = new UpdateSagaData
            {
                Id = Guid.NewGuid(),
                Originator = "Moo",
                OriginalMessageId = "MooId",
                MyProps = originalProp
            };

            await persister.Save(saga, null, null, null);
            var sagaData = await persister.Get<UpdateSagaData>(saga.Id, null, null);
            Assert.AreEqual(sagaData.MyProps, saga.MyProps);
            sagaData.MyProps = newProp;
            await persister.Update(sagaData, null, null);

            var updatedSaga = await persister.Get<UpdateSagaData>(saga.Id, null, null);

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