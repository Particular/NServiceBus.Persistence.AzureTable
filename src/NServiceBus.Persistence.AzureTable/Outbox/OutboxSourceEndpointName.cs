namespace NServiceBus.Persistence.AzureTable
{
    sealed class OutboxSourceEndpointName
    {
        public OutboxSourceEndpointName(string value) => Value = value;

        public string Value { get; }
    }
}
