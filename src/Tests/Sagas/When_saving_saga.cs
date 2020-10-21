namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Persisters
{
    using System;
    using Extensibility;
    using Microsoft.Azure.Cosmos.Table;
    using NUnit.Framework;

    public class When_saving_saga
    {
        [Test]
        public void Should_throw_exception_if_table_does_not_exist_and_auto_schema_is_off()
        {
            var connectionString = Testing.Utilities.GetEnvConfiguredConnectionStringForPersistence();

            var persister = new AzureSagaPersister(connectionString, false, false);
            var saga = new SaveSagaData
            {
                Id = Guid.NewGuid(),
                Originator = "Moo",
                OriginalMessageId = "MooId"
            };

            Assert.ThrowsAsync<StorageException>(async () => await persister.Save(saga, null, null, new ContextBag()));
        }

        [Test]
        public void Should_throw_when_date_is_invalid()
        {
            var connectionString = Testing.Utilities.GetEnvConfiguredConnectionStringForPersistence();

            var persister = new AzureSagaPersister(connectionString, false, false);
            var saga = new SaveSagaData
            {
                Id = Guid.NewGuid(),
                Originator = "Moo",
                OriginalMessageId = "MooId",
                DateTime = DateTime.MinValue
            };

            var exception = Assert.ThrowsAsync<Exception>(async () => await persister.Save(saga, null, null, new ContextBag()));
            var expected = $"Saga data of type '{typeof(SaveSagaData).FullName}' with DateTime property 'DateTime' has an invalid value '{saga.DateTime}'. Value cannot be null and must be equal to or greater than '{DictionaryTableEntityExtensions.StorageTableMinDateTime}'.";
            Assert.AreEqual(expected, exception.Message);
        }

        class SaveSagaData : IContainSagaData
        {
            public Guid Id { get; set; }
            public string Originator { get; set; }
            public string OriginalMessageId { get; set; }
            public DateTime DateTime { get; set; } = DateTime.UtcNow;
        }
    }
}