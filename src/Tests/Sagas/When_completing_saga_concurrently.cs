namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Persisters
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using Microsoft.Azure.Cosmos.Table;
    using NUnit.Framework;

    [TestFixture]
    public class When_completing_saga_concurrently
    {
        [Test]
        public async Task Should_remove_saga_data_and_throw_on_concurrent_complete()
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

            var retrievedSagaData = await persister.Get<CompleteSagaData>(sagaData.Id, null, bag);

            await persister.Complete(retrievedSagaData, null, bag);

            Assert.ThrowsAsync<StorageException>(async () => await persister.Complete(retrievedSagaData, null, bag));

            sagaData = await persister.Get<CompleteSagaData>(sagaData.Id, null, null);
            Assert.IsNull(sagaData);
        }
    }
}