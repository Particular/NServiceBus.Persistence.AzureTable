namespace NServiceBus.AzureStoragePersistence.ComponentTests.Persisters
{
    using System;
    using NServiceBus.Saga;
    using NServiceBus.SagaPersisters.Azure;
    using NUnit.Framework;

    public class When_updating_saga
    {
        [Test]
        public void Should_save_updated_properties()
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

            persister.Save(saga);
            var sagaData = persister.Get<UpdateSagaData>(saga.Id);
            Assert.AreEqual(sagaData.MyProps, saga.MyProps);
            sagaData.MyProps = newProp;
            persister.Update(sagaData);

            var updatedSaga = persister.Get<UpdateSagaData>(saga.Id);

            Assert.AreEqual(updatedSaga.MyProps, newProp);
        }
    }

    public class UpdateSagaData : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
        public string MyProps { get; set; }
    }

}