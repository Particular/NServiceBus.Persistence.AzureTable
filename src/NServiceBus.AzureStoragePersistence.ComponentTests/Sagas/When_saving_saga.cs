namespace NServiceBus.AzureStoragePersistence.ComponentTests.Persisters
{
    using System;
    using Microsoft.WindowsAzure.Storage;
    using SagaPersisters.Azure;
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

            Assert.Throws<StorageException>(async () => await persister.Save(saga, null, null, null));
        }
    }

    public class SaveSagaData : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }
}