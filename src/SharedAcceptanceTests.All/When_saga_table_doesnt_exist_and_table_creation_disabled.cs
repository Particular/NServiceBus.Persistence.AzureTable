namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Azure;
    using EndpointTemplates;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting.Support;
    using NUnit.Framework;
    using Testing;

    public partial class When_saga_table_doesnt_exist_and_table_creation_disabled : NServiceBusAcceptanceTest
    {
        const string TableThatDoesntExist = "doesnotexist";

        [SetUp]
        public Task Setup()
        {
            try
            {
                return SetupFixture.TableServiceClient.DeleteTableAsync(TableThatDoesntExist);
            }
            catch (RequestFailedException e) when (e.Status == (int)HttpStatusCode.NotFound)
            {
                return Task.CompletedTask;
            }
        }

        [Test]
        public void Should_throw_not_supported()
        {
            var exception = Assert.ThrowsAsync<MessageFailedException>(async () =>
                await Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithTableCreationDisabled>(b => b.When(session => session.SendLocal(new StartSagaMessage
                    {
                        SomeId = Guid.NewGuid()
                    })))
                    .Done(c => c.FailedMessages.Any())
                    .Run());

            Assert.That(exception.ScenarioContext.FailedMessages.Count, Is.EqualTo(1));
            StringAssert.Contains(
                ConnectionStringHelper.IsPremiumEndpoint(SetupFixture.ConnectionString)
                    ? "The specified resource does not exist."
                    : "The table specified does not exist",
                exception.FailedMessage.Exception.Message);
        }

        public class Context : ScenarioContext
        {
        }

        public class EndpointWithTableCreationDisabled : EndpointConfigurationBuilder
        {
            public EndpointWithTableCreationDisabled() =>
                EndpointSetup<DefaultServer>(c =>
                {
                    var sagaPersistence = c.UsePersistence<AzureTablePersistence, StorageType.Sagas>();
                    sagaPersistence.DefaultTable(TableThatDoesntExist);
                    sagaPersistence.DisableTableCreation();

                    var subscriptionStorage = c.UsePersistence<AzureTablePersistence, StorageType.Subscriptions>();
                    subscriptionStorage.DisableTableCreation();
                });

            public class SomeSaga : Saga<SomeSagaData>, IAmStartedByMessages<StartSagaMessage>
            {
                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    Data.SomeId = message.SomeId;
                    return Task.CompletedTask;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SomeSagaData> mapper) =>
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                          .ToSaga(s => s.SomeId);
            }

            public class SomeSagaData : ContainSagaData
            {
                public Guid SomeId { get; set; }
            }
        }

        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }
    }
}