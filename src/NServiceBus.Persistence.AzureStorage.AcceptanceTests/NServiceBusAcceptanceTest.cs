namespace NServiceBus.AcceptanceTests
{
    using System;
    using NUnit.Framework;
    using NUnit.Framework.Interfaces;

    public abstract partial class NServiceBusAcceptanceTest
    {
        const string PropertiesRecorderKey = "recorder";

        [SetUp]
        public void SetUpRecorder()
        {
            TestContext.CurrentContext.Test.Properties.Set(PropertiesRecorderKey, new AzureRequestRecorder());
        }

        [TearDown]
        public void TearDownRecorder()
        {
            var ctx = TestContext.CurrentContext;
            var recorder = (AzureRequestRecorder)ctx.Test.Properties.Get(PropertiesRecorderKey);

            if (ctx.Result.Outcome == ResultState.Error)
            {
                recorder.Print(Console.Out);
            }
            
            recorder.Dispose();
        }
    }
}