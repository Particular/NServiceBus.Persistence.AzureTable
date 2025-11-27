namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Data.Common;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Azure.Data.Tables;
    using Azure.Identity;
    using EndpointTemplates;
    using NUnit.Framework;
    using Testing;

    public class When_default_credentials_used : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_work()
        {
            if (ConnectionStringHelper.IsRunningWithEmulator(this.GetEnvConfiguredConnectionStringByCallerConvention()))
            {
                Assert.Ignore("This test uses DefaultAzureCredential which is not supported with the emulator.");
            }

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointUsingDefaultCredentials>(b => b.When(session => session.SendLocal(new StartSaga1
                {
                    DataId = Guid.NewGuid()
                })))
                .Done(c => c.SagaReceivedMessage)
                .Run();

            Assert.That(context.SagaReceivedMessage, Is.True);
        }

        public class Context : ScenarioContext
        {
            public bool SagaReceivedMessage { get; set; }
        }

        public class EndpointUsingDefaultCredentials : EndpointConfigurationBuilder
        {
            public EndpointUsingDefaultCredentials() =>
                EndpointSetup<DefaultServer>(config =>
                {
                    var builder = new DbConnectionStringBuilder
                    {
                        ConnectionString = this.GetEnvConfiguredConnectionStringByCallerConvention()
                    };
                    builder.TryGetValue("AccountEndpoint", out var accountEndpoint);

                    var baseUrlTemplate = $"https://{builder["AccountName"]}.{{0}}.{builder["EndpointSuffix"]}";
                    var tableServiceClient = new TableServiceClient(new Uri(string.Format(baseUrlTemplate, "table")), new DefaultAzureCredential());

                    var persistence = config.UsePersistence<AzureTablePersistence>();
                    persistence.UseTableServiceClient(tableServiceClient);
                });

            public class JustASaga : Saga<JustASagaData>, IAmStartedByMessages<StartSaga1>
            {
                public JustASaga(Context testContext) => this.testContext = testContext;

                public Task Handle(StartSaga1 message, IMessageHandlerContext context)
                {
                    Data.DataId = message.DataId;
                    testContext.SagaReceivedMessage = true;
                    MarkAsComplete();
                    return Task.CompletedTask;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<JustASagaData> mapper)
                    => mapper.MapSaga(s => s.DataId).ToMessage<StartSaga1>(m => m.DataId);

                readonly Context testContext;
            }

            public class JustASagaData : ContainSagaData
            {
                public virtual Guid DataId { get; set; }
            }
        }

        public class StartSaga1 : ICommand
        {
            public Guid DataId { get; set; }
        }
    }
}