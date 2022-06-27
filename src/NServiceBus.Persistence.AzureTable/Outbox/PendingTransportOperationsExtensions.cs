namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq.Expressions;
    using System.Reflection;
    using Transport;

    static class PendingTransportOperationsExtensions
    {
        static PendingTransportOperationsExtensions()
        {
            var field = typeof(PendingTransportOperations).GetField("operations",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var targetExp = Expression.Parameter(typeof(PendingTransportOperations), "target");
            var fieldExp = Expression.Field(targetExp, field);
            getter = Expression
                .Lambda<Func<PendingTransportOperations, ConcurrentStack<TransportOperation>>>(fieldExp, targetExp)
                .Compile();
        }

        public static void Clear(this PendingTransportOperations operations)
        {
            var collection = getter(operations);
            collection.Clear();
        }

        static readonly Func<PendingTransportOperations, ConcurrentStack<TransportOperation>> getter;
    }
}