namespace NServiceBus.AcceptanceTests
{
    using NUnit.Framework;

    static partial class Requires
    {
        public static void AzureStorageTable()
        {
            if (ConnectionStringHelper.IsPremiumEndpoint(SetupFixture.TableClient))
            {
                Assert.Ignore("Ignoring because it requires Azure Tables.");
            }
        }

        public static void AzureCosmosTable()
        {
            if (!ConnectionStringHelper.IsPremiumEndpoint(SetupFixture.TableClient))
            {
                Assert.Ignore("Ignoring because it requires Azure Tables.");
            }
        }
    }
}