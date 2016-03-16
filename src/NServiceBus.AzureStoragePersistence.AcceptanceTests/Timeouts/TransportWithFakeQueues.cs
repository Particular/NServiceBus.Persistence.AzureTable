
namespace NServiceBus.AcceptanceTests.Timeouts
{
    using System;
    using Settings;
    using NServiceBus.Transports;
    using System.Threading.Tasks;
    using Extensibility;
    using System.Collections.Generic;
    using Routing;
    using DelayedDelivery;

    public class TransportWithFakeQueues : TransportDefinition
    {
        protected override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
        {
           return new FakeTransportInfrastructure();
        }

        public override string ExampleConnectionStringForErrorMessage { get; } = string.Empty;
    }

    class FakeTransportInfrastructure : TransportInfrastructure
    {

        public override EndpointInstance BindToLocalEndpoint(EndpointInstance instance)
        {
            throw new NotImplementedException();
        }

        public override string ToTransportAddress(LogicalAddress logicalAddress)
        {
            throw new NotImplementedException();
        }

        public override TransportReceiveInfrastructure ConfigureReceiveInfrastructure()
        {
            throw new NotImplementedException();
        }

        public override TransportSendInfrastructure ConfigureSendInfrastructure()
        {
            throw new NotImplementedException();
        }

        public override TransportSubscriptionInfrastructure ConfigureSubscriptionInfrastructure()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<Type> DeliveryConstraints { get; } = new[] { typeof(DelayDeliveryWith) };

        public override TransportTransactionMode TransactionMode { get; } = TransportTransactionMode.None;

        public override OutboundRoutingPolicy OutboundRoutingPolicy { get; } = new OutboundRoutingPolicy(OutboundRoutingType.Unicast, OutboundRoutingType.Unicast, OutboundRoutingType.Unicast);


    }

    class FakeReceiver : IPushMessages
    {
        CriticalError criticalError;
        Exception throwCritical;

        public Task Init(Func<PushContext, Task> pipe, CriticalError criticalError, PushSettings settings)
        {
            this.criticalError = criticalError;
            return Task.FromResult(0);
        }

        public void Start(PushRuntimeSettings limitations)
        {
            if (throwCritical != null)
            {
                criticalError.Raise(throwCritical.Message, throwCritical);
            }
        }

        public Task Stop()
        {
            return Task.FromResult(0);
        }

        public FakeReceiver(Exception throwCritical)
        {
            this.throwCritical = throwCritical;
        }
    }

    class FakeQueueCreator : ICreateQueues
    {
        public Task CreateQueueIfNecessary(QueueBindings queueBindings, string identity)
        {
            return Task.FromResult(0);
        }
    }

    class FakeDispatcher : IDispatchMessages
    {
        public Task Dispatch(TransportOperations outgoingMessages, ContextBag context)
        {
            return Task.FromResult(0);
        }
    }
}