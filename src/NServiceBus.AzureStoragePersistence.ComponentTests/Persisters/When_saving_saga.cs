namespace NServiceBus.AzureStoragePersistence.ComponentTests.Persisters
{
    using System;
    using NServiceBus.Saga;
    using NServiceBus.SagaPersisters.Azure;
    using NUnit.Framework;

    public class When_saving_saga
    {
        [Test]
        public void Should_throw_exception_if_table_does_not_exist_and_auto_schema_is_off()
        {
            var connectionString = AzurePersistenceTests.GetConnectionString();

            var persister = new AzureSagaPersister(connectionString, false);
            var saga = new SaveSagaData
            {
                Id = Guid.NewGuid(),
                Originator = "Moo",
                OriginalMessageId = "MooId"
            };

            Assert.Throws<Microsoft.WindowsAzure.Storage.StorageException>(() => persister.Save(saga));
        }
    }

    public class SaveSagaData : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }

}