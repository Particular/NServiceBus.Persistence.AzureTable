using NServiceBus;
using NUnit.Framework;
using Particular.Approvals;
using PublicApiGenerator;

[TestFixture]
public class APIApprovals
{
    [Test]
    public void ApproveAzureStoragePersistence()
    {
        var publicApi = ApiGenerator.GeneratePublicApi(typeof(AzureStoragePersistence).Assembly, excludeAttributes: new[] { "System.Runtime.Versioning.TargetFrameworkAttribute" });
        Approver.Verify(publicApi);
    }

}