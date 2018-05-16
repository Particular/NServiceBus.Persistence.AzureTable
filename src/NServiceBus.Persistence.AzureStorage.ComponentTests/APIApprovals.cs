using System.Runtime.CompilerServices;
using NServiceBus;
using NServiceBus.Persistence.AzureStorage.ComponentTests;
using NUnit.Framework;
using PublicApiGenerator;

[TestFixture]
public class APIApprovals
{
    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ApproveAzureStorageQueueTransport()
    {
        var publicApi = ApiGenerator.GeneratePublicApi(typeof(AzureStoragePersistence).Assembly);
        TestApprover.Verify(publicApi);
    }
}