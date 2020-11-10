namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Persisters
{
    using System;
    using Timeout;
    using NUnit.Framework;

    public class When_using_AzureTimeoutStorageGuard
    {
        [TestCase("")]
        [TestCase(null)]
        public void Should_not_allow_invalid_connection_string(string connectionString)
        {
            Assert.Throws<ArgumentException>(() => AzureTimeoutStorageGuard.CheckConnectionString(connectionString));
        }

        [Test]
        public void Should_not_allow_catch_up_interval_less_than_1_second()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => AzureTimeoutStorageGuard.CheckCatchUpInterval(0));
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("1table")]
        [TestCase("aa")]
        [TestCase("aaaaaaaaaabbbbbbbbbbccccccccccddddddddddeeeeeeeeeeffffffffffgggg")] // 
        public void Should_not_allow_invalid_table_name(string tableName)
        {
            Assert.Throws<ArgumentException>(() => AzureTimeoutStorageGuard.CheckTableName(tableName));
        }

        [TestCase("")]
        [TestCase("invalid:key")]
        public void Should_not_allow_invalid_partition_key_scope(string partitionKeyScope)
        {
            Assert.Throws<ArgumentException>(() => AzureTimeoutStorageGuard.CheckPartitionKeyScope(partitionKeyScope));
        }
    }
}