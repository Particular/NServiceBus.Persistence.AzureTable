namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Persisters
{
    using System;
    using Extensibility;
    using Microsoft.WindowsAzure.Storage;
    using NUnit.Framework;

    public class When_saving_saga
    {
        [Test]
        public void Should_throw_exception_if_table_does_not_exist_and_auto_schema_is_off()
        {
            var connectionString = Testing.Utillities.GetEnvConfiguredConnectionStringForTransport();

            var persister = new AzureSagaPersister(connectionString, false);
            var saga = new SaveSagaData
            {
                Id = Guid.NewGuid(),
                Originator = "Moo",
                OriginalMessageId = "MooId"
            };

            Assert.ThrowsAsync<StorageException>(async () => await persister.Save(saga, null, null, new ContextBag()));
        }

        class SaveSagaData : IContainSagaData
        {
            public Guid Id { get; set; }
            public string Originator { get; set; }
            public string OriginalMessageId { get; set; }
        }
    }
}