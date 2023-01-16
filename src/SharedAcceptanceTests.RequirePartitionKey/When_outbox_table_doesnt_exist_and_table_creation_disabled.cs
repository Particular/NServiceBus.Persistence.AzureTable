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

    public partial class When_outbox_table_doesnt_exist_and_table_creation_disabled : NServiceBusAcceptanceTest
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
                    .WithEndpoint<EndpointWithOutbox>(b => b.When(session => session.SendLocal(new SomeCommand
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

        public class EndpointWithOutbox : EndpointConfigurationBuilder
        {
            public EndpointWithOutbox() =>
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnableOutbox();
                    c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;

                    var sagaPersistence = c.UsePersistence<AzureTablePersistence, StorageType.Outbox>();
                    sagaPersistence.DefaultTable(TableThatDoesntExist);

                    // Note that DefaultServer disables table creation as part of the default persistence configuration
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