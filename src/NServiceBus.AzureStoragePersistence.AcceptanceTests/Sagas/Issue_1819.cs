namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using System.Threading.Tasks;
    using NServiceBus.Sagas;

    public class Issue_1819 : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Run()
        {
            var testContext = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                    .WithEndpoint<Endpoint>(b => {
                            b.CustomConfig(endpointConfig => { endpointConfig.ExecuteTheseHandlersFirst(typeof(Endpoint.CatchAllMessageHandler)); });
                            b.When((instance, c) => instance.SendLocal(new StartSaga1 { ContextId = c.Id }));
                        })
                    .Done(c => (c.Saga1TimeoutFired && c.Saga2TimeoutFired) || c.SagaNotFound)
                    .Run(TimeSpan.FromSeconds(2000));

            Assert.IsFalse(testContext.SagaNotFound);
            Assert.IsTrue(testContext.Saga1TimeoutFired);
            Assert.IsTrue(testContext.Saga2TimeoutFired);
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public bool Saga1TimeoutFired { get; set; }
            public bool Saga2TimeoutFired { get; set; }
            public bool SagaNotFound { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class Saga1 : Saga<Saga1.Saga1Data>, IAmStartedByMessages<StartSaga1>, IHandleTimeouts<Saga1Timeout>, IHandleTimeouts<Saga2Timeout>
            {
                public Context Context { get; set; }

                public async Task Handle(StartSaga1 message, IMessageHandlerContext context)
                {
                    if (message.ContextId != Context.Id)
                    {
                        return;
                    }

                    await RequestTimeout(context, TimeSpan.FromSeconds(5), new Saga1Timeout { ContextId = Context.Id });
                    await RequestTimeout(context, new DateTime(2011, 10, 14, 23, 08, 0, DateTimeKind.Local), new Saga2Timeout { ContextId = Context.Id });
                }

                public Task Timeout(Saga1Timeout state, IMessageHandlerContext context)
                {
                    MarkAsComplete();

                    if (state.ContextId != Context.Id)
                    {
                        return Task.FromResult(0);
                    }

                    Context.Saga1TimeoutFired = true;
                    return Task.FromResult(0);
                }

                public Task Timeout(Saga2Timeout state, IMessageHandlerContext context)
                {
                    if (state.ContextId != Context.Id)
                    {
                        return Task.FromResult(0);
                    }

                    Context.Saga2TimeoutFired = true;
                    return Task.FromResult(0);
                }

                public class Saga1Data : ContainSagaData
                {
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<Saga1Data> mapper)
                {
                }
            }

            public class SagaNotFound : IHandleSagaNotFound
            {
                public Context Context { get; set; }

                public Task Handle(object message, IMessageProcessingContext context)
                {
                    if (((dynamic)message).ContextId != Context.Id)
                    {
                        return Task.FromResult(0);
                    }

                    Context.SagaNotFound = true;
                    return Task.FromResult(0);
                }
            }

            public class CatchAllMessageHandler : IHandleMessages<object>
            {
                public Task Handle(object message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class StartSaga1 : ICommand
        {
            public Guid ContextId { get; set; }
        }


        public class Saga1Timeout
        {
            public Guid ContextId { get; set; }
        }

        public class Saga2Timeout
        {
            public Guid ContextId { get; set; }
        }
    }
}