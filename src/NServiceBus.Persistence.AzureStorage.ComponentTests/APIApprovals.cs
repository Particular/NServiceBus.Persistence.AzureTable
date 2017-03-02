using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using ApiApprover;
using ApprovalTests.Reporters;
using NUnit.Framework;

[TestFixture]
public class APIApprovals
{
    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    [UseReporter(typeof(DiffReporter), typeof(AllFailingTestsClipboardReporter))]
    public void ApproveAzureStorageQueueTransport()
    {
        var combine = Path.Combine(TestContext.CurrentContext.TestDirectory, "NServiceBus.Persistence.AzureStorage.dll");
        var assembly = Assembly.LoadFile(combine);
        PublicApiApprover.ApprovePublicApi(assembly);
    }

}