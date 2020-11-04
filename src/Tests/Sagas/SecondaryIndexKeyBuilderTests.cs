namespace NServiceBus.Persistence.AzureTable.ComponentTests.Sagas
{
    using System.Threading.Tasks;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    [TestFixture]
    public class SecondaryIndexKeyBuilderTests
    {
        [Test]
        public void Should_build_index_key()
        {
            const string id = "C4D91B59-A407-4CDA-A689-60AA3C334699";

            var metadata = SagaMetadata.Create(typeof(TestSaga));
            metadata.TryGetCorrelationProperty(out var sagaProp);

            var key = SecondaryIndexKeyBuilder.BuildTableKey(typeof(SagaData), new SagaCorrelationProperty(sagaProp.Name, id));
            var expected = "Index_NServiceBus.Persistence.AzureTable.ComponentTests.Sagas.SecondaryIndexKeyBuilderTests+SagaData_AdditionalId_\"C4D91B59-A407-4CDA-A689-60AA3C334699\"";
            Assert.AreEqual(expected, key.PartitionKey);
            Assert.AreEqual(string.Empty, key.RowKey);
        }

        class TestSaga : Saga<SagaData>, IAmStartedByMessages<StartSagaMessage>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                return Task.CompletedTask;
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