namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Azure.Data.Tables;
    using EndpointTemplates;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using Persistence.AzureTable;

    public class When_custom_provider_registered : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_be_used()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithCustomProvider>(b => b.When(session => session.SendLocal(new StartSaga1
                {
                    DataId = Guid.NewGuid()
                })))
                .Done(c => c.SagaReceivedMessage)
                .Run();

            Assert.True(context.ProviderWasCalled);
        }

        public class Context : ScenarioContext
        {
            public bool SagaReceivedMessage { get; set; }
            public bool ProviderWasCalled { get; set; }
        }

        public class EndpointWithCustomProvider : EndpointConfigurationBuilder
        {
            public EndpointWithCustomProvider()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.RegisterComponents(c =>
                        c.AddSingleton<IProvideCloudTableClient>(provider => new CustomProvider(provider.GetRequiredService<Context>())));
                });
            }

            public class JustASaga : Saga<JustASagaData>, IAmStartedByMessages<StartSaga1>
            {
                public JustASaga(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(StartSaga1 message, IMessageHandlerContext context)
                {
                    Data.DataId = message.DataId;
                    testContext.SagaReceivedMessage = true;
                    MarkAsComplete();
                    return Task.CompletedTask;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<JustASagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSaga1>(m => m.DataId).ToSaga(s => s.DataId);
                }

                readonly Context testContext;
            }

            public class CustomProvider : IProvideCloudTableClient
            {
                public CustomProvider(Context testContext)
                {
                    this.testContext = testContext;
                }

                public TableServiceClient Client
                {
                    get
                    {
                        testContext.ProviderWasCalled = true;
                        return SetupFixture.TableClient;
                    }
                }

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