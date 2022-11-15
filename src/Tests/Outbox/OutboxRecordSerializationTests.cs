namespace NServiceBus.Persistence.AzureTable.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class OutboxRecordSerializationTests
    {

        [Test]
        public void Should_be_able_to_deserialize_from_older_outbox_record_format()
        {
            var oldRecord = @"[
  {
    ""MessageId"": ""83375df3-f409-4b24-8b69-f509eac5fb6a"",
    ""Options"": {},
    ""Body"": ""AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="",
    ""Headers"": {}
  }
]";

            Assert.DoesNotThrow(() => OutboxRecord.DeserializeStorageTransportOperations(oldRecord));
        }
    }
}