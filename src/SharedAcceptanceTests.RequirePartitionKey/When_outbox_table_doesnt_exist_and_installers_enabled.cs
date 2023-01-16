namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Azure;
    using EndpointTemplates;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting.Support;
    using NUnit.Framework;

    public partial class When_outbox_table_doesnt_exist_and_installers_enabled : NServiceBusAcceptanceTest
    {
        const string TableToBeCreated = "tabletobecreated";

        [SetUp]
        [TearDown]
        public async Task TryDeleteTestTable()
        {
            try
            {
                await SetupFixture.TableServiceClient.DeleteTableAsync(TableToBeCreated);
            }
            catch (RequestFailedException e) when (e.Status == (int)HttpStatusCode.NotFound)
            {
            }
        }

        [Test]
        public async Task Should_create_table()
        {
            var settings = new RunSettings();
            settings.Set("allowTableCreation", true);

            await Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithOutbox>(b => b.When(session => session.SendLocal(new SomeCommand
                    {
                        SomeId = Guid.NewGuid()
                    })))
                    .Done(c => c.CommandReceived)
                    .Run(settings);

            var tableCreated = false;
            await foreach (var table in SetupFixture.TableServiceClient.QueryAsync(t => t.Name == TableToBeCreated))
            {
                tableCreated = true;
            }

            Assert.That(tableCreated, Is.True);
        }

        public class Context : ScenarioContext
        {
            public bool CommandReceived { get; set; }
        }

        public class EndpointWithOutbox : EndpointConfigurationBuilder
        {
            public EndpointWithOutbox() =>
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnableOutbox();
                    c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;

                    var sagaPersistence = c.UsePersistence<AzureTablePersistence, StorageType.Outbox>();
                    sagaPersistence.DefaultTable(TableToBeCreated);
                });

            public class MyCommandHandler : IHandleMessages<SomeCommand>
            {
                readonly Context testContext;

                public MyCommandHandler(Context testContext) => this.testContext = testContext;

                public Task Handle(SomeCommand message, IMessageHandlerContext context)
                {
                    testContext.CommandReceived = true;
                    return Task.CompletedTask;
                }
            }
        }

        public class SomeCommand : ICommand
        {
            public Guid SomeId { get; set; }
        }
    }
}