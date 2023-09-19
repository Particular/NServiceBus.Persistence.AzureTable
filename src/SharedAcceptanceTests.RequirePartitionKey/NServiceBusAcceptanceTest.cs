namespace NServiceBus.AcceptanceTests
{
    using NUnit.Framework;

    public abstract partial class NServiceBusAcceptanceTest
    {
        [TearDown]
        public void Teardown()
        {
        }
    }
}