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
            var runSettings = new RunSettings();
            runSettings.AllowTableCreation();

            await Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithOutboxAndInstallersOn>(b => b.When(session => session.SendLocal(new SomeCommand
                    {
                        SomeId = Guid.NewGuid()
                    })))
                    .Done(c => c.CommandReceived)
                    .Run(runSettings);

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

        public class EndpointWithOutboxAndInstallersOn : EndpointConfigurationBuilder
        {
            public EndpointWithOutboxAndInstallersOn() =>
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnableOutbox();
                    c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;

                    //Note: that EnabledInstallers is on by default in the TransactionSessionDefaultServer

                    var outboxPersistence = c.UsePersistence<AzureTablePersistence, StorageType.Outbox>();
                    outboxPersistence.DefaultTable(TableToBeCreated);
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