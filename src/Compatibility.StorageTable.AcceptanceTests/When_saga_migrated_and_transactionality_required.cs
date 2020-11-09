namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using Extensibility;
    using Sagas;
    using Microsoft.Azure.Cosmos.Table;
    using Persistence.AzureTable.Previous;
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

            var previousSagaData = new EndpointWithSagaThatWasMigrated.MigratedSagaData
            {
                Id = sagaId,
                OriginalMessageId = "",
                Originator = "",
                SomeId = correlationPropertyValue
            };

            var sagaCorrelationProperty = new SagaCorrelationProperty("SomeId", correlationPropertyValue);
            await PersisterUsingSecondaryIndexes.Save(previousSagaData, sagaCorrelationProperty, null, new ContextBag());

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithSagaThatWasMigrated>(b => b.When(session => session.SendLocal(new ContinueSagaMessage
                {
                    SomeId = correlationPropertyValue,
                    TableRowKey = myTableRowKey
                })))
                .Done(c => c.SagaIsDone && c.HandlerIsDone)
                .Run();

            var myEntity = GetByRowKey<EndpointWithSagaThatWasMigrated.MigratedSagaData>(myTableRowKey.ToString());

            Assert.IsNotNull(myEntity);
            Assert.AreEqual("MyCustomData", myEntity["Data"].StringValue);
            Assert.AreEqual(sagaId, context.SagaId);
        }

        [Test]
        public async Task Should_work_with_sagaid_header_if_present()
        {
            Requires.AzureTables();

            var correlationPropertyValue = Guid.NewGuid();
            var sagaId = Guid.NewGuid();
            var myTableRowKey = Guid.NewGuid();

            var previousSagaData = new EndpointWithSagaThatWasMigrated.MigratedSagaData
            {
                Id = sagaId,
                OriginalMessageId = "",
                Originator = "",
                SomeId = correlationPropertyValue
            };

            var sagaCorrelationProperty = new SagaCorrelationProperty("SomeId", correlationPropertyValue);
            await PersisterUsingSecondaryIndexes.Save(previousSagaData, sagaCorrelationProperty, null, new ContextBag());

            // making sure there is no secondary to lookup
            var partitionRowKeyTuple = SecondaryIndexKeyBuilder.BuildTableKey(typeof(EndpointWithSagaThatWasMigrated.MigratedSagaData), sagaCorrelationProperty);
            var secondaryIndexEntry = GetByPartitionKey<EndpointWithSagaThatWasMigrated.MigratedSagaData>(partitionRowKeyTuple.PartitionKey);
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
                    },options);
                }))
                .Done(c => c.SagaIsDone && c.HandlerIsDone)
                .Run();

            var myEntity = GetByRowKey<EndpointWithSagaThatWasMigrated.MigratedSagaData>(myTableRowKey.ToString());

            Assert.IsNotNull(myEntity);
            Assert.AreEqual("MyCustomData", myEntity["Data"].StringValue);
            Assert.AreEqual(sagaId, context.SagaId);
        }

        public class Context : ScenarioContext
        {
            public bool SagaIsDone { get; set; }
            public bool HandlerIsDone { get; set; }
            public Guid SagaId { get; set; }
        }

        public class EndpointWithSagaThatWasMigrated : EndpointConfigurationBuilder
        {
            public EndpointWithSagaThatWasMigrated()
            {
                EndpointSetup<DefaultServer>(c =>
                    c.Pipeline.Register(typeof(PartitionKeyProviderBehavior), "Provides a partition key by deriving it from the saga id"));
            }

            class PartitionKeyProviderBehavior : Behavior<IIncomingLogicalMessageContext>
            {
                private IProvidePartitionKeyFromSagaId providePartitionKeyFromSagaId;

                public PartitionKeyProviderBehavior(IProvidePartitionKeyFromSagaId providePartitionKeyFromSagaId)
                {
                    this.providePartitionKeyFromSagaId = providePartitionKeyFromSagaId;
                }

                public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
                {
                    if (context.Message.Instance is ContinueSagaMessage continueSagaMessage)
                    {
                        await providePartitionKeyFromSagaId
                            .SetPartitionKey<MigratedSagaData>(context, new SagaCorrelationProperty(nameof(continueSagaMessage.SomeId), continueSagaMessage.SomeId))
                            .ConfigureAwait(false);
                    }

                    await next().ConfigureAwait(false);
                }
            }

            public class SagaWithMigratedData : Saga<MigratedSagaData>, IAmStartedByMessages<StartSagaMessage>, IAmStartedByMessages<ContinueSagaMessage>
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
                    Data.SomeId = message.SomeId;

                    testContext.SagaId = Data.Id;
                    testContext.SagaIsDone = true;
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

            public class ContinueMessageHandler : IHandleMessages<ContinueSagaMessage>
            {
                public ContinueMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(ContinueSagaMessage message, IMessageHandlerContext context)
                {
                    var session = context.SynchronizedStorageSession.AzureTablePersistenceSession();

                    var entity = new MyTableEntity
                    {
                        RowKey = message.TableRowKey.ToString(),
                        PartitionKey = session.PartitionKey,
                        Data = "MyCustomData"
                    };
                    session.Batch.Add(TableOperation.Insert(entity));
                    testContext.HandlerIsDone = true;
                    return Task.CompletedTask;
                }

                private Context testContext;
            }

            public class MyTableEntity : TableEntity
            {
                public string Data { get; set; }
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

            public Guid TableRowKey { get; set; }
        }
    }
}