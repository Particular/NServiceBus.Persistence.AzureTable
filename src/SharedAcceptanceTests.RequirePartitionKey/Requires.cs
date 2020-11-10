namespace NServiceBus.AcceptanceTests
{
    using NUnit.Framework;

    static partial class Requires
    {
        public static void AzureTables()
        {
            if (ConnectionStringHelper.IsPremiumEndpoint(SetupFixture.TableClient))
            {
                Assert.Ignore("Ignoring because it requires Azure Tables.");
            }
        }
    }
}