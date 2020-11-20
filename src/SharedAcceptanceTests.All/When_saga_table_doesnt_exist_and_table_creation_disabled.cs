namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus;
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Support;
    using Testing;
    using EndpointTemplates;
    using NUnit.Framework;

    public partial class When_saga_table_doesnt_exist_and_table_creation_disabled : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw_not_supported()
        {
            var exception = Assert.ThrowsAsync<MessageFailedException>(async () =>
                await Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithSchemaCreationOff>(b => b.When(session => session.SendLocal(new StartSagaMessage
                    {
                        SomeId = Guid.NewGuid()
                    })))
                    .Done(c => c.FailedMessages.Any())
                    .Run());

            Assert.AreEqual(1, exception.ScenarioContext.FailedMessages.Count);
            StringAssert.Contains(
                ConnectionStringHelper.IsPremiumEndpoint(SetupFixture.TableClient)
                    ? "The specified resource does not exist."
                    : "Element 0 in the batch returned an unexpected response code.",
                exception.FailedMessage.Exception.Message);
        }

        public class Context : ScenarioContext
        {
        }

        public class EndpointWithSchemaCreationOff : EndpointConfigurationBuilder
        {
            public EndpointWithSchemaCreationOff()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    var sagaPersistence = c.UsePersistence<AzureTablePersistence, StorageType.Sagas>();
                    sagaPersistence.DefaultTable("doesnotexist");
                    sagaPersistence.DisableTableCreation();

                    var subscriptionStorage = c.UsePersistence<AzureTablePersistence, StorageType.Subscriptions>();
                    subscriptionStorage.DisableTableCreation();
                });
            }

            public class SomeSaga : Saga<SomeSagaData>, IAmStartedByMessages<StartSagaMessage>
            {
                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    Data.SomeId = message.SomeId;
                    return Task.CompletedTask;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SomeSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }
            }

            public class SomeSagaData : IContainSagaData
            {
                public Guid Id { get; set; }
                public string Originator { get; set; }
                public string OriginalMessageId { get; set; }
                public Guid SomeId { get; set; }
            }
        }

        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }
    }
}