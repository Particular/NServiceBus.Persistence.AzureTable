namespace NServiceBus.AcceptanceTests;

using AcceptanceTesting.Support;
using Azure.Data.Tables;

public partial class TestSuiteConstraints
{
    readonly TableServiceClient tableServiceClient;

    public TestSuiteConstraints() { }

    public TestSuiteConstraints(TableServiceClient tableServiceClient) => this.tableServiceClient = tableServiceClient;

    public bool SupportsDtc => false;
    public bool SupportsCrossQueueTransactions => true;
    public bool SupportsNativePubSub => true;
    public bool SupportsDelayedDelivery => true;
    public bool SupportsOutbox => true;
    public bool SupportsPurgeOnStartup => true;
    public IConfigureEndpointTestExecution CreateTransportConfiguration() => new ConfigureEndpointAcceptanceTestingTransport(SupportsNativePubSub, SupportsDelayedDelivery);
    public IConfigureEndpointTestExecution CreatePersistenceConfiguration() => new ConfigureAzureTablePersistence(tableServiceClient);
}