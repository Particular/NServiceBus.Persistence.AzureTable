namespace NServiceBus.Persistence.AzureTable.Tests
{
    using Logging;
    using NUnit.Framework;
    using Testing;

    /// <summary>
    /// Makes sure we are always using the <see cref="TestingLoggerFactory"/> so that the
    /// default logger is never instantiated.
    /// </summary>
    [SetUpFixture]
    public class LoggingSetupFixture
    {
        [OneTimeSetUp]
        public void SetUp() => LogManager.Use<TestingLoggerFactory>();
    }
}