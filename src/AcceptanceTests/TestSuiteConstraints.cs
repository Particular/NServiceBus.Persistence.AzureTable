namespace NServiceBus.AcceptanceTests
{
    using AcceptanceTesting.Support;

    public partial class TestSuiteConstraints
    {
        public bool SupportsDtc { get; } = false;
        public bool SupportsCrossQueueTransactions { get; } = false;
        public bool SupportsNativePubSub { get; } = false;
        public bool SupportsNativeDeferral { get; } = false;
        public bool SupportsOutbox { get; } = false;
        public IConfigureEndpointTestExecution CreateTransportConfiguration() => new ConfigureAcceptanceTestingTransport(SupportsNativePubSub, SupportsNativeDeferral);
        public IConfigureEndpointTestExecution CreatePersistenceConfiguration() => new ConfigureEndpointAzureStoragePersistence();
    }
}