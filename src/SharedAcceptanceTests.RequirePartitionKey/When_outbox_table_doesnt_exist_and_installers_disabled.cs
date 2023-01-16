namespace NServiceBus.AcceptanceTests
{
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

    public partial class When_outbox_table_doesnt_exist_and_installers_disabled : NServiceBusAcceptanceTest
    {
        const string TableThatDoesntExist = "doesnotexist";

        [SetUp]
        [TearDown]
        public async Task TryDeleteTestTable()
        {
            try
            {
                await SetupFixture.TableServiceClient.DeleteTableAsync(TableThatDoesntExist);
            }
            catch (RequestFailedException e) when (e.Status == (int)HttpStatusCode.NotFound)
            {
            }
        }

        [Test]
        public void Should_throw_not_supported()
        {
            var exception = Assert.ThrowsAsync<MessageFailedException>(async () =>
                await Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithInstallersOff>(b => b.When(session => session.SendLocal(new SomeCommand
                    {
                        SomeId = Guid.NewGuid()
                    })))
                    .Done(c => c.FailedMessages.Any())
                    .Run());

            Assert.AreEqual(1, exception.ScenarioContext.FailedMessages.Count);
            StringAssert.Contains(
                ConnectionStringHelper.IsPremiumEndpoint(SetupFixture.ConnectionString)
                    ? "The specified resource does not exist."
                    : "The table specified does not exist",
                exception.FailedMessage.Exception.Message);
        }

        public class Context : ScenarioContext
        {
        }

        public class EndpointWithInstallersOff : EndpointConfigurationBuilder
        {
            public EndpointWithInstallersOff() =>
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnableOutbox();
                    c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;

                    // so that we don't have to create a new endpoint template
                    c.GetSettings().Set("Installers.Enable", false);

                    var sagaPersistence = c.UsePersistence<AzureTablePersistence, StorageType.Outbox>();
                    sagaPersistence.DefaultTable(TableThatDoesntExist);
                });

            public class MyCommandHandler : IHandleMessages<SomeCommand>
            {
                public Task Handle(SomeCommand message, IMessageHandlerContext context) => Task.CompletedTask;
            }
        }

        public class SomeCommand : ICommand
        {
            public Guid SomeId { get; set; }
        }
    }
}