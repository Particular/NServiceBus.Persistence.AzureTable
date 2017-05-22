namespace NServiceBus.AcceptanceTests.PubSub
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using System.Threading.Tasks;

    public class When_publishing_on_brokers : NServiceBusAcceptanceTest
    {
        [Test]
        [Ignore("Currently used transport ASQ is not broker")]
        public async Task Should_be_delivered_to_allsubscribers_without_the_need_for_config()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<CentralizedPublisher>
                    (b => b.When(c => c.IsSubscriptionProcessedForSub1 && c.IsSubscriptionProcessedForSub2, bus => bus.Publish(new MyEvent())))
                    .WithEndpoint<CentralizedSubscriber1>(b => b.When((session, c) =>
                    {
                        c.IsSubscriptionProcessedForSub1 = true;

                        return Task.FromResult(0);
                    }))
                    .WithEndpoint<CentralizedSubscriber2>(b => b.When((bus, c) =>
                    {
                        c.IsSubscriptionProcessedForSub2 = true;

                        return Task.FromResult(0);
                    }))
                    .Done(c => c.Subscriber1GotTheEvent && c.Subscriber2GotTheEvent)
                    .Run();

            Assert.True(context.Subscriber1GotTheEvent);
            Assert.True(context.Subscriber2GotTheEvent);
        }

        public class Context : ScenarioContext
        {
            public bool Subscriber1GotTheEvent { get; set; }
            public bool Subscriber2GotTheEvent { get; set; }

            public bool IsSubscriptionProcessedForSub1 { get; set; }
            public bool IsSubscriptionProcessedForSub2 { get; set; }
        }

        public class CentralizedPublisher : EndpointConfigurationBuilder
        {
            public CentralizedPublisher()
            {
                EndpointSetup<DefaultPublisher>();
            }
        }

        public class CentralizedSubscriber1 : EndpointConfigurationBuilder
        {
            public CentralizedSubscriber1()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }

                public Task Handle(MyEvent messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    Context.Subscriber1GotTheEvent = true;

                    return Task.FromResult(0);
                }
            }
        }

        public class CentralizedSubscriber2 : EndpointConfigurationBuilder
        {
            public CentralizedSubscriber2()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }

                public Task Handle(MyEvent messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    Context.Subscriber2GotTheEvent = true;

                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class MyEvent : IEvent
        {
        }
    }
}