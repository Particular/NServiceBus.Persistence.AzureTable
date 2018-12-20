using NServiceBus;
using NUnit.Framework;
using Particular.Approvals;
using PublicApiGenerator;

[TestFixture]
public class APIApprovals
{
    [Test]
    public void ApproveAzureStorageQueueTransport()
    {
        var publicApi = ApiGenerator.GeneratePublicApi(typeof(AzureStoragePersistence).Assembly);
        Approver.Verify(publicApi);
    }
}