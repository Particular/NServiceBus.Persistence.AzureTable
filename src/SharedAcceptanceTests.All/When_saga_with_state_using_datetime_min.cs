namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus;
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Support;
    using EndpointTemplates;
    using NUnit.Framework;

    public partial class When_saga_with_state_using_datetime_min : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw()
        {
            var exception = Assert.ThrowsAsync<MessageFailedException>(async () =>
                await Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithSagaWithDateTimeMin>(b => b.When(session => session.SendLocal(new StartSagaMessage
                    {
                        SomeId = Guid.NewGuid()
                    })))
                    .Done(c => c.FailedMessages.Any())
                    .Run());

            Assert.AreEqual(1, exception.ScenarioContext.FailedMessages.Count);
            StringAssert.Contains(
                "with DateTime property 'DateTime' has an invalid value",
                exception.FailedMessage.Exception.Message);
        }

        public class Context : ScenarioContext
        {
        }

        public class EndpointWithSagaWithDateTimeMin : EndpointConfigurationBuilder
        {
            public EndpointWithSagaWithDateTimeMin() => EndpointSetup<DefaultServer>();

            public class SagaWithDateTimeMin : Saga<SagaDataWithDateTimeMin>, IAmStartedByMessages<StartSagaMessage>
            {
                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    Data.DateTime = DateTime.MinValue;
                    return Task.CompletedTask;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaDataWithDateTimeMin> mapper) =>
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
            }

            public class SagaDataWithDateTimeMin : ContainSagaData
            {
                public Guid SomeId { get; set; }
                public DateTime DateTime { get; set; }
            }
        }

        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }
    }
}