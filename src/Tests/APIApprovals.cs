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
        var publicApi = typeof(AzureStoragePersistence).Assembly.GeneratePublicApi(new ApiGeneratorOptions
        {
            ExcludeAttributes = new[] { "System.Runtime.Versioning.TargetFrameworkAttribute" }
        });
        Approver.Verify(publicApi);
    }
}