namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class When_receiving_that_should_start_a_saga : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_start_saga_if_a_interception_handler_has_been_invoked()
        {
            await Scenario.Define<SagaEndpointContext>(c => { c.InterceptSaga = true; })
                .WithEndpoint<SagaEndpoint>(b => b.When(session => session.SendLocal(new StartSagaMessage { SomeId = Guid.NewGuid().ToString() })))
                .Done(context => context.InterceptingHandlerCalled)
                .Repeat(r => r.For(Transports.Default))
                .Should(c =>
                {
                    Assert.True(c.InterceptingHandlerCalled, "The intercepting handler should be called");
                    Assert.False(c.SagaStarted, "The saga should not have been started since the intercepting handler stops the pipeline");
                })
                .Run();
        }

        [Test]
        public async Task Should_start_the_saga_and_call_messagehandlers()
        {
            await Scenario.Define<SagaEndpointContext>()
                .WithEndpoint<SagaEndpoint>(b => b.When(session => session.SendLocal(new StartSagaMessage { SomeId = Guid.NewGuid().ToString() })))
                .Done(context => context.InterceptingHandlerCalled && context.SagaStarted)
                .Repeat(r => r.For(Transports.Default))
                .Should(c =>
                {
                    Assert.True(c.InterceptingHandlerCalled, "The message handler should be called");
                    Assert.True(c.SagaStarted, "The saga should have been started");
                })
                .Run();
        }

        public class SagaEndpointContext : ScenarioContext
        {
            public bool InterceptingHandlerCalled { get; set; }

            public bool SagaStarted { get; set; }

            public bool InterceptSaga { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>(b => b.ExecuteTheseHandlersFirst(typeof(InterceptingHandler)));
            }

            public class TestSaga03 : Saga<TestSaga03.TestSagaData03>, IAmStartedByMessages<StartSagaMessage>
            {
                public SagaEndpointContext Context { get; set; }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    Context.SagaStarted = true;
                    Data.SomeId = message.SomeId;
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData03> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }

                public class TestSagaData03 : ContainSagaData
                {
                    public virtual string SomeId { get; set; }
                }
            }


            public class InterceptingHandler : IHandleMessages<StartSagaMessage>
            {
                public SagaEndpointContext TestContext { get; set; }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    TestContext.InterceptingHandlerCalled = true;

                    if (TestContext.InterceptSaga)
                        context.DoNotContinueDispatchingCurrentMessageToHandlers();

                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class StartSagaMessage : ICommand
        {
            public string SomeId { get; set; }
        }
    }
}
