namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Azure;
    using Azure.Data.Tables;
    using EndpointTemplates;
    using NServiceBus;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Persistence.AzureTable;
    using Pipeline;

    public class When_participating_in_saga_conversations : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_optionally_provide_transactionality_based_on_sagaid()
        {
            var correlationPropertyValue = Guid.NewGuid();
            var myTableRowKey = Guid.NewGuid();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithSagaThatWasMigrated>(b => b.When(session => session.SendLocal(new ContinueSagaMessage
                {
                    SomeId = correlationPropertyValue,
                    TableRowKey = myTableRowKey
                })))
                .Done(c => c.SagaIsDone && c.HandlerIsDone)
                .Run();

            var myEntity = GetByRowKey(myTableRowKey);

            Assert.That(myEntity, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(myEntity.TryGetValue("Data", out var entityValue), Is.True);
                Assert.That(entityValue, Is.EqualTo(context.SagaId.ToString()));
            });
        }

        [Test]
        public async Task Should_optionally_provide_transactionality_based_on_sagaheader()
        {
            var correlationPropertyValue = Guid.NewGuid();
            var myTableRowKey = Guid.NewGuid();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithSagaThatWasMigrated>(b => b.When(session => session.SendLocal(new StartSagaMessage
                {
                    SomeId = correlationPropertyValue,
                    TableRowKey = myTableRowKey
                })))
                .Done(c => c.SagaIsDone && c.HandlerIsDone)
                .Run();

            var myEntity = GetByRowKey(myTableRowKey);

            Assert.That(myEntity, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(myEntity.TryGetValue("Data", out var entityValue), Is.True);
                Assert.That(entityValue, Is.EqualTo(context.SagaId.ToString()));
            });
        }

        static TableEntity GetByRowKey(Guid sagaId)
        {
            var table = SetupFixture.TableClient;

            // table scan but still probably the easiest way to do it, otherwise we would have to take the partition key into account which complicates things because this test is shared
            //var query = new TableQuery<DynamicTableEntity>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, sagaId.ToString()));

            try
            {
                var tableEntity = table.Query<TableEntity>(entity => entity.RowKey == sagaId.ToString()).FirstOrDefault();
                return tableEntity;
            }
            catch (RequestFailedException e) when (e.Status == (int)HttpStatusCode.NotFound)
            {
                return null;
            }
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
                {
                    c.Pipeline.Register(typeof(PartitionPartitionKeyCleanerBehavior), "Cleans partition keys out");
                    c.Pipeline.Register(new ProvidePartitionKeyBasedOnSagaIdBehavior.Registration());
                });

            class PartitionPartitionKeyCleanerBehavior : IBehavior<ITransportReceiveContext, ITransportReceiveContext>,
                IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
            {
                public Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, Task> next)
                {
                    // to make it work in all test projects
                    context.Extensions.Remove<TableEntityPartitionKey>();
                    return next(context);
                }

                public Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
                {
                    // to make it work in all test projects
                    context.Extensions.Remove<TableEntityPartitionKey>();
                    return next(context);
                }
            }

            class ProvidePartitionKeyBasedOnSagaIdBehavior : Behavior<IIncomingLogicalMessageContext>
            {
                readonly IProvidePartitionKeyFromSagaId providePartitionKeyFromSagaId;

                public ProvidePartitionKeyBasedOnSagaIdBehavior(IProvidePartitionKeyFromSagaId providePartitionKeyFromSagaId) => this.providePartitionKeyFromSagaId = providePartitionKeyFromSagaId;

                public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
                {
                    // to make it work in all test projects
                    context.Extensions.Remove<TableEntityPartitionKey>();

                    if (context.Message.Instance is ContinueSagaMessage continueSagaMessage)
                    {
                        await providePartitionKeyFromSagaId
                            .SetPartitionKey<CustomSagaData>(context, new SagaCorrelationProperty(nameof(continueSagaMessage.SomeId), continueSagaMessage.SomeId));
                    }

                    if (context.Message.Instance is StartSagaMessage startSagaMessage)
                    {
                        await providePartitionKeyFromSagaId
                            .SetPartitionKey<CustomSagaData>(context, new SagaCorrelationProperty(nameof(startSagaMessage.SomeId), startSagaMessage.SomeId));
                    }

                    await next();
                }

                public class Registration : RegisterStep
                {
                    public Registration() : base(nameof(ProvidePartitionKeyBasedOnSagaIdBehavior),
                        typeof(ProvidePartitionKeyBasedOnSagaIdBehavior),
                        "Populates the partition key") => InsertBeforeIfExists(nameof(LogicalOutboxBehavior));
                }
            }

            public class CustomSaga : Saga<CustomSagaData>, IAmStartedByMessages<StartSagaMessage>, IAmStartedByMessages<ContinueSagaMessage>
            {
                public CustomSaga(Context testContext) => this.testContext = testContext;

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    Data.SomeId = message.SomeId;

                    var options = new SendOptions();
                    options.SetHeader(Headers.SagaId, Data.Id.ToString());
                    options.RouteToThisEndpoint();

                    return context.Send(new ContinueSagaMessage { SomeId = message.SomeId, TableRowKey = message.TableRowKey }, options);
                }

                public Task Handle(ContinueSagaMessage message, IMessageHandlerContext context)
                {
                    Data.SomeId = message.SomeId;

                    testContext.SagaId = Data.Id;
                    testContext.SagaIsDone = true;
                    return Task.CompletedTask;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<CustomSagaData> mapper) =>
                    mapper.MapSaga(s => s.SomeId)
                        .ToMessage<StartSagaMessage>(m => m.SomeId)
                        .ToMessage<ContinueSagaMessage>(m => m.SomeId);

                readonly Context testContext;
            }

            public class ContinueMessageHandler : IHandleMessages<ContinueSagaMessage>
            {
                public ContinueMessageHandler(Context testContext) => this.testContext = testContext;

                public Task Handle(ContinueSagaMessage message, IMessageHandlerContext context)
                {
                    var session = context.SynchronizedStorageSession.AzureTablePersistenceSession();

                    var entity = new MyTableEntity
                    {
                        RowKey = message.TableRowKey.ToString(),
                        PartitionKey = session.PartitionKey,
                        Data = session.PartitionKey
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

            public class CustomSagaData : ContainSagaData
            {
                public Guid SomeId { get; set; }
            }
        }

        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
            public Guid TableRowKey { get; set; }
        }

        public class ContinueSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }

            public Guid TableRowKey { get; set; }
        }
    }
}