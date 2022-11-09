namespace NServiceBus.Persistence.AzureTable.Release_2x
{
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SagaEntityTypeAttribute : Attribute
    {
        public Type SagaEntityType { get; set; }
    }
}