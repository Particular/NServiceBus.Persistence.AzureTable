namespace NServiceBus.Persistence.AzureTable.Release_2x
{
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    sealed class SagaEntityTypeAttribute : Attribute
    {
        public Type SagaEntityType { get; set; }
    }
}