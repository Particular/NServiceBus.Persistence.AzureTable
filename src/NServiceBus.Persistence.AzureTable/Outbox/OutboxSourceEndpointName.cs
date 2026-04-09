namespace NServiceBus.Persistence.AzureTable
{
    sealed class OutboxSourceEndpointName(string value)
    {
        public string Value { get; } = value;
    }
}
