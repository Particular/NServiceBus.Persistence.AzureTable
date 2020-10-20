namespace NServiceBus.Persistence.AzureStorage
{
    using Microsoft.Azure.Documents;

    // Needed for the logical outbox to have the right partition key to complete an outbox transaction when SetAsDispatched() is invoked
    class SetAsDispatchedHolder
    {
        public PartitionKey PartitionKey  { get; set; }
        public TableHolder TableHolder { get; set; }
    }
}