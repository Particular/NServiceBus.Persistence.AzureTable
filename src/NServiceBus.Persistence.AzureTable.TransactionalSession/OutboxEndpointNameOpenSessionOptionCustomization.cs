namespace NServiceBus.TransactionalSession
{
    sealed class OutboxEndpointNameOpenSessionOptionCustomization : IOpenSessionOptionsCustomization
    {
        readonly string endpointName;

        public OutboxEndpointNameOpenSessionOptionCustomization(string endpointName) => this.endpointName = endpointName;

        public void Apply(OpenSessionOptions options) =>
            options.Metadata[AzureTableControlMessageBehavior.OutboxEndpointNameHeaderKey] = endpointName;
    }
}
