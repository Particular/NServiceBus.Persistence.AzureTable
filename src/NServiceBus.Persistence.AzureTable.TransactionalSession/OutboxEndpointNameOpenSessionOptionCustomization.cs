namespace NServiceBus.TransactionalSession
{
    sealed class OutboxEndpointNameOpenSessionOptionCustomization(string endpointName) : IOpenSessionOptionsCustomization
    {
        public void Apply(OpenSessionOptions options) =>
            options.Metadata[AzureTableControlMessageBehavior.OutboxEndpointNameHeaderKey] = endpointName;
    }
}
