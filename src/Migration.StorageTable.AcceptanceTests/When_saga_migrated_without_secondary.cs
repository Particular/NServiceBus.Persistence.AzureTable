namespace NServiceBus.AcceptanceTests
{
    using System.Linq;
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using Extensibility;
    using Persistence.AzureTable.Previous;
    using Sagas;

    public class When_saga_migrated_without_secondary : MigrationAcceptanceTest
    {
        [Test]
        public async Task Should_find_via_table_scan_if_enabled()
        {
            Requires.AzureTables();

            var correlationPropertyValue = Guid.NewGuid();
            var sagaId = Guid.NewGuid();

            var previousSagaData = new EndpointWithSagaThatWasMigrated.MigratedSagaData
            {
                Id = sagaId,
                OriginalMessageId = "",
                Originator = "",
                SomeId = correlationPropertyValue
            };

            var sagaCorrelationProperty = new SagaCorrelationProperty("SomeId", correlationPropertyValue);
            await PersisterUsingSecondaryIndexes.Save(previousSagaData, sagaCorrelationProperty, null, new ContextBag());

            var partitionRowKeyTuple = SecondaryIndexKeyBuilder.BuildTableKey(typeof(EndpointWithSagaThatWasMigrated.MigratedSagaData), sagaCorrelationProperty);
            var secondaryIndexEntry = GetByPartitionKey<EndpointWithSagaThatWasMigrated.MigratedSagaData>(partitionRowKeyTuple.PartitionKey);
            await DeleteEntity<EndpointWithSagaThatWasMigrated.MigratedSagaData>(secondaryIndexEntry);

            using (var recorder = new AzureRequestRecorder())
            {
                var context = await Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithSagaThatWasMigrated>(b =>
                    {
                        b.CustomConfig(c =>
                        {
                            var sagaPersistence = c.UsePersistence<AzureTablePersistence, StorageType.Sagas>();
                            var migration = sagaPersistence.Migration();
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
                    get.Contains($"$filter=SomeId%20eq%20guid%27{correlationPropertyValue}%27&$select=PartitionKey%2CRowKey%2CTimestamp"))
                    .ToArray();

                CollectionAssert.IsNotEmpty(getWithFilter);
                Assert.AreEqual(sagaId, context.SagaId);
            }
        }

        [Test]
        public async Task Should_create_new_saga_and_not_issue_table_scan_if_not_enabled()
        {
            Requires.AzureTables();

            var correlationPropertyValue = Guid.NewGuid();
            var sagaId = Guid.NewGuid();

            var previousSagaData = new EndpointWithSagaThatWasMigrated.MigratedSagaData
            {
                Id = sagaId,
                OriginalMessageId = "",
                Originator = "",
                SomeId = correlationPropertyValue
            };

            var sagaCorrelationProperty = new SagaCorrelationProperty("SomeId", correlationPropertyValue);
            await PersisterUsingSecondaryIndexes.Save(previousSagaData, sagaCorrelationProperty, null, new ContextBag());

            var partitionRowKeyTuple = SecondaryIndexKeyBuilder.BuildTableKey(typeof(EndpointWithSagaThatWasMigrated.MigratedSagaData), sagaCorrelationProperty);
            var secondaryIndexEntry = GetByPartitionKey<EndpointWithSagaThatWasMigrated.MigratedSagaData>(partitionRowKeyTuple.PartitionKey);
            await DeleteEntity<EndpointWithSagaThatWasMigrated.MigratedSagaData>(secondaryIndexEntry);

            using (var recorder = new AzureRequestRecorder())
            {
                var context = await Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithSagaThatWasMigrated>(b => b.When(session =>
                        session.SendLocal(new StartSagaMessage
                        {
                            SomeId = correlationPropertyValue
                        })))
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
        }

        public class Context : ScenarioContext
        {
            public bool Done { get; set; }
            public Guid SagaId { get; set; }
        }

        public class EndpointWithSagaThatWasMigrated : EndpointConfigurationBuilder
        {
            public EndpointWithSagaThatWasMigrated()
            {
                EndpointSetup<DefaultServer>();
            }

            public class SagaWithMigratedData : Saga<MigratedSagaData>, IAmStartedByMessages<StartSagaMessage>, IHandleMessages<ContinueSagaMessage>
            {
                public SagaWithMigratedData(Context testContext)
                {
                    this.testContext = testContext;
                }

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

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MigratedSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                    mapper.ConfigureMapping<ContinueSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }

                private readonly Context testContext;
            }

            public class MigratedSagaData : IContainSagaData
            {
                public Guid Id { get; set; }
                public string Originator { get; set; }
                public string OriginalMessageId { get; set; }
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