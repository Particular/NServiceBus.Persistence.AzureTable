namespace NServiceBus.AcceptanceTests
{
    using AcceptanceTesting.Support;

    public partial class TestSuiteConstraints
    {
        public bool SupportsDtc { get; } = false;
        public bool SupportsCrossQueueTransactions { get; } = true;
        public bool SupportsNativePubSub { get; } = true;
        public bool SupportsDelayedDelivery { get; } = true;
        public bool SupportsOutbox { get; } = false;
        public IConfigureEndpointTestExecution CreateTransportConfiguration() => new ConfigureEndpointAcceptanceTestingTransport(SupportsNativePubSub, SupportsDelayedDelivery);
        public IConfigureEndpointTestExecution CreatePersistenceConfiguration() => new ConfigureEndpointAzureTablePersistence();
    }
}