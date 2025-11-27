namespace NServiceBus.AcceptanceTests;

using NUnit.Framework;
using Testing;

static partial class Requires
{
    public static void AzureStorageTable()
    {
        if (ConnectionStringHelper.IsPremiumEndpoint(SetupFixture.ConnectionString))
        {
            Assert.Ignore("Ignoring because it requires Azure Tables.");
        }
    }

    public static void AzureCosmosTable()
    {
        if (!ConnectionStringHelper.IsPremiumEndpoint(SetupFixture.ConnectionString))
        {
            Assert.Ignore("Ignoring because it requires Azure Tables.");
        }
    }
}