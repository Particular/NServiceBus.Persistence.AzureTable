namespace NServiceBus.AzureStoragePersistence.ComponentTests.Sagas
{
    using System;
    using Saga;
    using SagaPersisters.Azure.SecondaryIndeces;
    using NUnit.Framework;

    public class IndexDefinitionTests
    {
        readonly IndexDefintion index;

        private class SagaData : ContainSagaData
        {
            [Unique]
            public string AdditionalId { get; set; }
        }

        public IndexDefinitionTests()
        {
            index = IndexDefintion.Get(typeof(SagaData));
        }

        [Test]
        public void Should_access_value_properly()
        {
            const string id = "FF4E1C4E-D2F2-4601-8D8E-CB3E91872043";
            var sagaData = new SagaData
            {
                AdditionalId = id
            };

            Assert.AreEqual(id, index.Accessor(sagaData));
        }

        [Test]
        public void Should_validate_property()
        {
            Assert.Throws<ArgumentException>(() => index.ValidateProperty("AdditionalId_"));

            index.ValidateProperty("AdditionalId");
        }

        [Test]
        public void Should_build_index_key()
        {
            const string id = "C4D91B59-A407-4CDA-A689-60AA3C334699";
            var key = index.BuildTableKey(id);
            Assert.AreEqual("Index_NServiceBus.AzureStoragePersistence.ComponentTests.Sagas.IndexDefinitionTests+SagaData_AdditionalId_\"C4D91B59-A407-4CDA-A689-60AA3C334699\"", key.PartitionKey);
            Assert.AreEqual("", key.RowKey);
        }
    }
}