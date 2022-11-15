namespace NServiceBus.AcceptanceTests
{
    using System.Linq;
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Azure.Core;
    using Azure.Data.Tables;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_sagas_used : NServiceBusAcceptanceTest
    {
        AzureRequestRecorder recorder;
        TableServiceClient tableServiceClient;

        [SetUp]
        public void Setup()
        {
            var tableClientOptions = new TableClientOptions();
            recorder = new AzureRequestRecorder();
            tableClientOptions.AddPolicy(recorder, HttpPipelinePosition.PerCall);
            tableServiceClient = new TableServiceClient(SetupFixture.ConnectionString, tableClientOptions);
        }

        [Test]
        public async Task Should_never_issue_table_scans()
        {
            var correlationPropertyValue = Guid.NewGuid();

            await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithCustomProvider>(b =>
                {
                    b.CustomConfig(c =>
                    {
                        var sagaPersistence = c.UsePersistence<AzureTablePersistence, StorageType.Sagas>();
                        sagaPersistence.UseTableServiceClient(tableServiceClient);
                    });
                    b.When(session => session.SendLocal(new StartSaga1 { DataId = correlationPropertyValue }));
                })
                .Done(c => c.SagaReceivedMessage)
                .Run();

            recorder.Print(Console.Out);

            var gets = recorder.Requests.Where(r => r.ToLower().Contains("get"));
            var getWithFilter = gets.Where(get =>
                    get.Contains($"$select=PartitionKey%2CRowKey&$filter=DataId%20eq%20guid%27{correlationPropertyValue}%27"))
                .ToArray();

            CollectionAssert.IsEmpty(getWithFilter);
        }

        public class Context : ScenarioContext
        {
            public bool SagaReceivedMessage { get; set; }
        }

        public class EndpointWithCustomProvider : EndpointConfigurationBuilder
        {
            public EndpointWithCustomProvider() => EndpointSetup<DefaultServer>();

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
                    => mapper.ConfigureMapping<StartSaga1>(m => m.DataId).ToSaga(s => s.DataId);

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