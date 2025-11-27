namespace NServiceBus.AcceptanceTests;

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AcceptanceTesting;
using Azure;
using Configuration.AdvancedExtensibility;
using EndpointTemplates;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NUnit.Framework;
using Testing;

public partial class When_saga_table_doesnt_exist_and_installers_disabled : NServiceBusAcceptanceTest
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
                .WithEndpoint<EndpointWithInstallersOff>(b => b.When(session => session.SendLocal(new StartSagaMessage { SomeId = Guid.NewGuid() })))
                .Done(c => c.FailedMessages.Any())
                .Run());

        Assert.That(exception.ScenarioContext.FailedMessages, Has.Count.EqualTo(1));
        Assert.That(
            exception.FailedMessage.Exception.Message,
            Does.Contain(ConnectionStringHelper.IsPremiumEndpoint(SetupFixture.ConnectionString)
                ? "The specified resource does not exist."
                : "The table specified does not exist"));
    }

    public class Context : ScenarioContext
    {
    }

    public class EndpointWithInstallersOff : EndpointConfigurationBuilder
    {
        public EndpointWithInstallersOff() =>
            EndpointSetup<DefaultServer>(c =>
            {
                // so that we don't have to create a new endpoint template
                c.GetSettings().Set("Installers.Enable", false);

                var sagaPersistence = c.UsePersistence<AzureTablePersistence, StorageType.Sagas>();
                sagaPersistence.DefaultTable(TableThatDoesntExist);

                //it is on by default
                sagaPersistence.DisableTableCreation();

                var subscriptionStorage = c.UsePersistence<AzureTablePersistence, StorageType.Subscriptions>();
            });

        public class SomeSaga : Saga<SomeSagaData>, IAmStartedByMessages<StartSagaMessage>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                Data.SomeId = message.SomeId;
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SomeSagaData> mapper) =>
                mapper.MapSaga(s => s.SomeId).ToMessage<StartSagaMessage>(m => m.SomeId);
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