namespace NServiceBus.AcceptanceTests.Sagas
{
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;

    [TestFixture]
    public class When_a_finder_exists
    {
        [Test]
        public async Task Should_use_it_to_find_saga()
        {
            var context = await Scenario.Define<Context>()
                   .WithEndpoint<SagaEndpoint>(b => b.When(instance => instance.SendLocal(new StartSagaMessage())))
                   .Done(c => c.FinderUsed)
                   .Run();

            Assert.True(context.FinderUsed);
        }

        public class Context : ScenarioContext
        {
            public bool FinderUsed { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            class CustomFinder : IFindSagas<TestSaga.SagaData>.Using<StartSagaMessage>
            {
                public Context Context { get; set; }

                public Task<TestSaga.SagaData> FindBy(StartSagaMessage message, SynchronizedStorageSession storageSession, ReadOnlyContextBag context)
                {
                    Context.FinderUsed = true;
                    return Task.FromResult((TestSaga.SagaData)null);
                }
            }

            public class TestSaga : Saga<TestSaga.SagaData>, IAmStartedByMessages<StartSagaMessage>
            {
                public Context Context { get; set; }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
                {
                }

                public class SagaData : ContainSagaData
                {
                }
            }

        }

        public class StartSagaMessage : IMessage
        {
        }
    }
}