namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using AcceptanceTesting;
    using Azure;
    using Azure.Data.Tables;
    using EndpointTemplates;
    using NUnit.Framework;
    using Sagas;
    using Persistence.AzureTable.Release_2x;
    using Persistence.AzureTable;
    using Pipeline;

    public class When_saga_migrated_and_transactionality_required : CompatibilityAcceptanceTest
    {
        [Test]
        public async Task Should_work_by_looking_up_sagaid()
        {
            Requires.AzureTables();

            var correlationPropertyValue = Guid.NewGuid();
            var sagaId = Guid.NewGuid();
            var myTableRowKey = Guid.NewGuid();

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
                    SomeId = correlationPropertyValue,
                    TableRowKey = myTableRowKey
                })))
                .Done(c => c.SagaIsDone && c.HandlerIsDone)
                .Run();

            var myEntity = await GetByRowKey<EndpointWithSagaThatWasMigrated.MigratedSagaData>(myTableRowKey.ToString());

            Assert.That(myEntity, Is.Not.Null);
            Assert.That(myEntity["Data"], Is.EqualTo("MyCustomData"));
            Assert.That(context.SagaId, Is.EqualTo(sagaId));
        }

        [Test]
        public async Task Should_work_with_sagaid_header_if_present()
        {
            Requires.AzureTables();

            var correlationPropertyValue = Guid.NewGuid();
            var sagaId = Guid.NewGuid();
            var myTableRowKey = Guid.NewGuid();

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

            // making sure there is no secondary to lookup
            var partitionRowKeyTuple = SecondaryIndexKeyBuilder.BuildTableKey(typeof(EndpointWithSagaThatWasMigrated.MigratedSagaData), sagaCorrelationProperty);
            var secondaryIndexEntry = await GetByPartitionKey<EndpointWithSagaThatWasMigrated.MigratedSagaData>(partitionRowKeyTuple.PartitionKey);
            await DeleteEntity<EndpointWithSagaThatWasMigrated.MigratedSagaData>(secondaryIndexEntry);

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithSagaThatWasMigrated>(b => b.When(session =>
                {
                    var options = new SendOptions();
                    options.SetHeader(Headers.SagaId, sagaId.ToString());
                    options.RouteToThisEndpoint();

                    return session.Send(new ContinueSagaMessage
                    {
                        SomeId = correlationPropertyValue,
                        TableRowKey = myTableRowKey
                    }, options);
                }))
                .Done(c => c.SagaIsDone && c.HandlerIsDone)
                .Run();

            var myEntity = await GetByRowKey<EndpointWithSagaThatWasMigrated.MigratedSagaData>(myTableRowKey.ToString());

            Assert.That(myEntity, Is.Not.Null);
            Assert.That(myEntity["Data"], Is.EqualTo("MyCustomData"));
            Assert.That(context.SagaId, Is.EqualTo(sagaId));
        }

        public class Context : ScenarioContext
        {
            public bool SagaIsDone { get; set; }
            public bool HandlerIsDone { get; set; }
            public Guid SagaId { get; set; }
        }

        public class EndpointWithSagaThatWasMigrated : EndpointConfigurationBuilder
        {
            public EndpointWithSagaThatWasMigrated() =>
                EndpointSetup<DefaultServer>(c =>
                    c.Pipeline.Register(typeof(PartitionKeyProviderBehavior), "Provides a partition key by deriving it from the saga id"));

            class PartitionKeyProviderBehavior : Behavior<IIncomingLogicalMessageContext>
            {
                public PartitionKeyProviderBehavior(IProvidePartitionKeyFromSagaId providePartitionKeyFromSagaId)
                    => this.providePartitionKeyFromSagaId = providePartitionKeyFromSagaId;

                public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
                {
                    if (context.Message.Instance is ContinueSagaMessage continueSagaMessage)
                    {
                        await providePartitionKeyFromSagaId
                            .SetPartitionKey<MigratedSagaData>(context, new SagaCorrelationProperty(nameof(continueSagaMessage.SomeId), continueSagaMessage.SomeId));
                    }

                    await next();
                }

                readonly IProvidePartitionKeyFromSagaId providePartitionKeyFromSagaId;
            }

            public class SagaWithMigratedData : Saga<MigratedSagaData>, IAmStartedByMessages<StartSagaMessage>, IAmStartedByMessages<ContinueSagaMessage>
            {
                public SagaWithMigratedData(Context testContext)
                    => this.testContext = testContext;

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    // would have been called this way on older persistence
                    Data.SomeId = message.SomeId;

                    return context.SendLocal(new ContinueSagaMessage { SomeId = message.SomeId });
                }

                public Task Handle(ContinueSagaMessage message, IMessageHandlerContext context)
                {
                    Data.SomeId = message.SomeId;

                    testContext.SagaId = Data.Id;
                    testContext.SagaIsDone = true;
                    return Task.CompletedTask;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MigratedSagaData> mapper) =>
                    mapper.MapSaga(s => s.SomeId)
                        .ToMessage<StartSagaMessage>(m => m.SomeId)
                        .ToMessage<ContinueSagaMessage>(m => m.SomeId);

                readonly Context testContext;
            }

            public class ContinueMessageHandler : IHandleMessages<ContinueSagaMessage>
            {
                public ContinueMessageHandler(Context testContext)
                    => this.testContext = testContext;

                public Task Handle(ContinueSagaMessage message, IMessageHandlerContext context)
                {
                    var session = context.SynchronizedStorageSession.AzureTablePersistenceSession();

                    var entity = new MyTableEntity
                    {
                        RowKey = message.TableRowKey.ToString(),
                        PartitionKey = session.PartitionKey,
                        Data = "MyCustomData"
                    };
                    session.Batch.Add(new TableTransactionAction(TableTransactionActionType.Add, entity));
                    testContext.HandlerIsDone = true;
                    return Task.CompletedTask;
                }

                readonly Context testContext;
            }

            public class MyTableEntity : ITableEntity
            {
                public string Data { get; set; }
                public string PartitionKey { get; set; }
                public string RowKey { get; set; }
                public DateTimeOffset? Timestamp { get; set; }
                public ETag ETag { get; set; }
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

            public Guid TableRowKey { get; set; }
        }
    }
}