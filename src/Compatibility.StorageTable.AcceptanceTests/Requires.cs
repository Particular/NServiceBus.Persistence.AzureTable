namespace NServiceBus.AcceptanceTests
{
    using NUnit.Framework;
    using Testing;

    static partial class Requires
    {
        public static void AzureTables()
        {
            if (ConnectionStringHelper.IsPremiumEndpoint(SetupFixture.ConnectionString))
            {
                Assert.Ignore("Ignoring because it requires Azure Tables.");
            }
        }
    }
}