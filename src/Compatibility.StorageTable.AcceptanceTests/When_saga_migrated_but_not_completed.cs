namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using Persistence.AzureTable.Release_2x;
    using Sagas;

    public class When_saga_migrated_but_not_completed : CompatibilityAcceptanceTest
    {
        [Test]
        public async Task Should_preserve_secondary_index()
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

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithSagaThatWasMigrated>(b => b.When(session => session.SendLocal(new ContinueSagaMessage
                {
                    SomeId = correlationPropertyValue
                })))
                .Done(c => c.Done)
                .Run();

            var sagaEntity = await GetByRowKey<EndpointWithSagaThatWasMigrated.MigratedSagaData>(context.SagaId.ToString());
            var partitionRowKeyTuple = SecondaryIndexKeyBuilder.BuildTableKey(typeof(EndpointWithSagaThatWasMigrated.MigratedSagaData), sagaCorrelationProperty);
            var secondaryIndexEntry = await GetByPartitionKey<EndpointWithSagaThatWasMigrated.MigratedSagaData>(partitionRowKeyTuple.PartitionKey);

            Assert.Multiple(() =>
            {
                Assert.That(sagaEntity.ContainsKey("NServiceBus_2ndIndexKey"), Is.True, "Secondary index property should be preserved");
                Assert.That(secondaryIndexEntry, Is.Not.Null);
                Assert.That(context.SagaId, Is.EqualTo(sagaId));
            });
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