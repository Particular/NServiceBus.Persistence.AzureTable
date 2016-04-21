namespace NServiceBus.AzureStoragePersistence.ComponentTests.Sagas
{
    using System.Threading.Tasks;
    using SagaPersisters.AzureStoragePersistence.SecondaryIndices;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    public class SecondaryIndexKeyBuilderTests
    {
        [Test]
        public void Should_build_index_key()
        {
            const string id = "C4D91B59-A407-4CDA-A689-60AA3C334699";

            var metadata = SagaMetadata.Create(typeof(TestSaga));
            SagaMetadata.CorrelationPropertyMetadata sagaProp;
            metadata.TryGetCorrelationProperty(out sagaProp);

            var key = SecondaryIndexKeyBuilder.BuildTableKey(typeof(SagaData), new SagaCorrelationProperty(sagaProp.Name, id));
            Assert.AreEqual("Index_NServiceBus.AzureStoragePersistence.ComponentTests.Sagas.SecondaryIndexKeyBuilderTests+SagaData_AdditionalId_\"C4D91B59-A407-4CDA-A689-60AA3C334699\"", key.PartitionKey);
            Assert.AreEqual("", key.RowKey);
        }

        class TestSaga : Saga<SagaData>, IAmStartedByMessages<StartSagaMessage>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                return TaskEx.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureMapping<StartSagaMessage>(m => m.AdditionalId).ToSaga(a => a.AdditionalId);
            }
        }

        class StartSagaMessage
        {
            public string AdditionalId { get; set; }
        }

        class SagaData : ContainSagaData
        {
            public string AdditionalId { get; set; }
        }
    }
}