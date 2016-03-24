namespace NServiceBus.AzureStoragePersistence.ComponentTests.Sagas
{
    using System;
    using NServiceBus.Sagas;
    using SagaPersisters.Azure.SecondaryIndeces;
    using NUnit.Framework;
    using System.Threading.Tasks;

    public class IndexDefinitionTests
    {
        [Test]
        public void Should_validate_property()
        {
            var metadata = SagaMetadata.Create(typeof(TestSaga));
            SagaMetadata.CorrelationPropertyMetadata sagaProp;
            metadata.TryGetCorrelationProperty(out sagaProp);

            var index = IndexDefinition.Get(typeof(SagaData), new SagaCorrelationProperty(sagaProp.Name, Guid.NewGuid().ToString()));

            Assert.Throws<ArgumentException>(() => index.ValidateProperty("AdditionalId_"));

            index.ValidateProperty("AdditionalId");
        }

        [Test]
        public void Should_build_index_key()
        {
            const string id = "C4D91B59-A407-4CDA-A689-60AA3C334699";

            var metadata = SagaMetadata.Create(typeof(TestSaga));
            SagaMetadata.CorrelationPropertyMetadata sagaProp;
            metadata.TryGetCorrelationProperty(out sagaProp);

            var index = IndexDefinition.Get(typeof(SagaData), new SagaCorrelationProperty(sagaProp.Name, id));

            var key = index.BuildTableKey(id);
            Assert.AreEqual("Index_NServiceBus.AzureStoragePersistence.ComponentTests.Sagas.IndexDefinitionTests+SagaData_AdditionalId_\"C4D91B59-A407-4CDA-A689-60AA3C334699\"", key.PartitionKey);
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

        private class SagaData : ContainSagaData
        {
            public string AdditionalId { get; set; }
        }
    }
}