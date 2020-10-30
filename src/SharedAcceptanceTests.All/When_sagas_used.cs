namespace NServiceBus.AcceptanceTests
{
    using System.Linq;
    using Persistence.AzureStorage.ComponentTests;
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_sagas_used : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_never_issue_table_scans()
        {
            var correlationPropertyValue = Guid.NewGuid();

            using (var recorder = new AzureRequestRecorder())
            {
                var context = await Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithCustomProvider>(b => b.When(session => session.SendLocal(new StartSaga1
                    {
                        DataId = correlationPropertyValue
                    })))
                    .Done(c => c.SagaReceivedMessage)
                    .Run();

                recorder.Print(Console.Out);

                var gets = recorder.Requests.Where(r => r.ToLower().Contains("get"));
                var getWithFilter = gets.Where(get =>
                        get.Contains($"$filter=DataId%20eq%20guid%27{correlationPropertyValue}%27&$select=PartitionKey%2CRowKey%2CTimestamp"))
                    .ToArray();

                CollectionAssert.IsEmpty(getWithFilter);
            }
        }

        public class Context : ScenarioContext
        {
            public bool SagaReceivedMessage { get; set; }
        }

        public class EndpointWithCustomProvider : EndpointConfigurationBuilder
        {
            public EndpointWithCustomProvider()
            {
                EndpointSetup<DefaultServer>();
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