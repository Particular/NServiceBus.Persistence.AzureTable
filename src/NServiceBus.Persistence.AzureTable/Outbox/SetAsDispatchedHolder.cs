namespace NServiceBus.Persistence.AzureTable
{
    // Needed for the logical outbox to have the right partition key to complete an outbox transaction when SetAsDispatched() is invoked
    sealed class SetAsDispatchedHolder
    {
        public OutboxRecord Record { get; set; }
        public TableClientHolder TableClientHolder { get; set; }
        public TableEntityPartitionKey PartitionKey { get; set; }
    }
}