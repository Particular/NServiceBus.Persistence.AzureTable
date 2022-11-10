namespace NServiceBus.AcceptanceTests
{
    using System.Linq;
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using AcceptanceTesting;
    using Azure.Core;
    using Azure.Data.Tables;
    using EndpointTemplates;
    using NUnit.Framework;
    using Persistence.AzureTable.Release_2x;
    using Sagas;

    public class When_saga_migrated_without_secondary : CompatibilityAcceptanceTest
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
        public async Task Should_find_via_table_scan_if_enabled()
        {
            Requires.AzureTables();

            var correlationPropertyValue = Guid.NewGuid();
            var sagaId = Guid.NewGuid();

            var previousSagaData = new EndpointWithSagaThatWasMigrated.MigratedSagaDataTableEntity
            {
                RowKey = sagaId.ToString(),
                PartitionKey = sagaId.ToString(),
                OriginalMessageId = "",
                Originator = "",
                SomeId = correlationPropertyValue
            };

            var sagaCorrelationProperty = new SagaCorrelationProperty("SomeId", correlationPropertyValue);
            await SaveSagaInOldFormat(previousSagaData, sagaCorrelationProperty);

            var partitionRowKeyTuple = SecondaryIndexKeyBuilder.BuildTableKey(typeof(EndpointWithSagaThatWasMigrated.MigratedSagaData), sagaCorrelationProperty);
            var secondaryIndexEntry = await GetByPartitionKey<EndpointWithSagaThatWasMigrated.MigratedSagaData>(partitionRowKeyTuple.PartitionKey);
            await DeleteEntity<EndpointWithSagaThatWasMigrated.MigratedSagaData>(secondaryIndexEntry);

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithSagaThatWasMigrated>(b =>
                {
                    b.CustomConfig(c =>
                    {
                        var sagaPersistence = c.UsePersistence<AzureTablePersistence, StorageType.Sagas>();
                        sagaPersistence.UseTableServiceClient(tableServiceClient);
                        var migration = sagaPersistence.Compatibility();
                        migration.AllowSecondaryKeyLookupToFallbackToFullTableScan();
                    });
                    b.When(session =>
                        session.SendLocal(new ContinueSagaMessage
                        {
                            SomeId = correlationPropertyValue
                        }));
                })
                .Done(c => c.Done)
                .Run();

            recorder.Print(Console.Out);

            var gets = recorder.Requests.Where(r => r.ToLower().Contains("get"));
            var getWithFilter = gets.Where(get =>
                                        get.Contains($"$select=PartitionKey%2CRowKey&$filter=SomeId%20eq%20guid%27{correlationPropertyValue}%27"))
                .ToArray();

            CollectionAssert.IsNotEmpty(getWithFilter);
            Assert.AreEqual(sagaId, context.SagaId);
        }

        [Test]
        public async Task Should_create_new_saga_and_not_issue_table_scan_if_not_enabled()
        {
            Requires.AzureTables();

            var correlationPropertyValue = Guid.NewGuid();
            var sagaId = Guid.NewGuid();

            var previousSagaData = new EndpointWithSagaThatWasMigrated.MigratedSagaDataTableEntity
            {
                RowKey = sagaId.ToString(),
                PartitionKey = sagaId.ToString(),
                OriginalMessageId = "",
                Originator = "",
                SomeId = correlationPropertyValue
            };

            var sagaCorrelationProperty = new SagaCorrelationProperty("SomeId", correlationPropertyValue);
            await SaveSagaInOldFormat(previousSagaData, sagaCorrelationProperty);

            var partitionRowKeyTuple = SecondaryIndexKeyBuilder.BuildTableKey(typeof(EndpointWithSagaThatWasMigrated.MigratedSagaData), sagaCorrelationProperty);
            var secondaryIndexEntry = await GetByPartitionKey<EndpointWithSagaThatWasMigrated.MigratedSagaData>(partitionRowKeyTuple.PartitionKey);
            await DeleteEntity<EndpointWithSagaThatWasMigrated.MigratedSagaData>(secondaryIndexEntry);

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithSagaThatWasMigrated>(b =>
                {
                    b.CustomConfig(cfg =>
                    {
                        var sagaPersistence = cfg.UsePersistence<AzureTablePersistence, StorageType.Sagas>();
                        sagaPersistence.UseTableServiceClient(tableServiceClient);
                    });
                    b.When(session =>
                        session.SendLocal(new StartSagaMessage { SomeId = correlationPropertyValue }));
                })
                .Done(c => c.Done)
                .Run();

            recorder.Print(Console.Out);

            var gets = recorder.Requests.Where(r => r.ToLower().Contains("get"));
            var getWithFilter = gets.Where(get =>
                    get.Contains($"$filter=SomeId%20eq%20guid%27{correlationPropertyValue}%27&$select=PartitionKey%2CRowKey%2CTimestamp"))
                .ToArray();

            CollectionAssert.IsEmpty(getWithFilter);
            Assert.AreNotEqual(sagaId, context.SagaId);
        }

        public class Context : ScenarioContext
        {
            public bool Done { get; set; }
            public Guid SagaId { get; set; }
        }

        public class EndpointWithSagaThatWasMigrated : EndpointConfigurationBuilder
        {
            public EndpointWithSagaThatWasMigrated() => EndpointSetup<DefaultServer>();

            public class SagaWithMigratedData : Saga<MigratedSagaData>, IAmStartedByMessages<StartSagaMessage>, IHandleMessages<ContinueSagaMessage>
            {
                public SagaWithMigratedData(Context testContext) => this.testContext = testContext;

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    // would have been called this way on older persistence
                    Data.SomeId = message.SomeId;

                    return context.SendLocal(new ContinueSagaMessage { SomeId = message.SomeId });
                }

                public Task Handle(ContinueSagaMessage message, IMessageHandlerContext context)
                {
                    testContext.SagaId = Data.Id;
                    testContext.Done = true;
                    return Task.CompletedTask;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MigratedSagaData> mapper) =>
                    mapper.MapSaga(s => s.SomeId)
                        .ToMessage<StartSagaMessage>(m => m.SomeId)
                        .ToMessage<ContinueSagaMessage>(m => m.SomeId);

                readonly Context testContext;
            }

            public class MigratedSagaData : ContainSagaData
            {
                public Guid SomeId { get; set; }
            }

            [SagaEntityType(SagaEntityType = typeof(MigratedSagaData))]
            public class MigratedSagaDataTableEntity : SagaDataTableEntity
            {
                public Guid SomeId { get; set; }
            }
        }

        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }

        public class ContinueSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }
    }
}