namespace NServiceBus.Persistence.AzureTable.ComponentTests.Persisters
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    [Category("Azure")]
    public class When_using_AzureSubscriptionStorageGuard
    {
        [TestCase("")]
        [TestCase(null)]
        public void Should_not_allow_invalid_connection_string(string connectionString)
        {
            Assert.Throws<ArgumentException>(() => AzureSubscriptionStorageGuard.CheckConnectionString(connectionString));
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("1table")]
        [TestCase("aa")]
        [TestCase("aaaaaaaaaabbbbbbbbbbccccccccccddddddddddeeeeeeeeeeffffffffffgggg")] //
        public void Should_not_allow_invalid_table_name(string tableName)
        {
            Assert.Throws<ArgumentException>(() => AzureSubscriptionStorageGuard.CheckTableName(tableName));
        }
    }
}