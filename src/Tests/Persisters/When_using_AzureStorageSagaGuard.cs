namespace NServiceBus.Persistence.AzureTable.ComponentTests.Persisters
{
    using System;
    using NUnit.Framework;

    public class When_using_AzureStorageSagaGuard
    {
        [TestCase("")]
        [TestCase(null)]
        public void Should_not_allow_invalid_connection_string(string connectionString)
        {
            Assert.Throws<ArgumentException>(() => AzureStorageSagaGuard.CheckConnectionString(connectionString));
        }
    }
}