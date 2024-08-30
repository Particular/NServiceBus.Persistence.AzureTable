namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Persistence.AzureTable;
    using System.Net;
    using Azure;
    using Azure.Data.Tables;
    using ITableEntity = Azure.Data.Tables.ITableEntity;

    public class When_using_transactional_session : NServiceBusAcceptanceTest
    {
        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_send_messages_and_store_document_in_synchronized_session_on_transactional_session_commit(bool outboxEnabled)
        {
            var entityRowId = Guid.NewGuid().ToString();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<AnEndpoint>(s => s.When(async (_, ctx) =>
                {
                    using var scope = ctx.ServiceProvider.CreateScope();
                    using var transactionalSession = scope.ServiceProvider.GetRequiredService<ITransactionalSession>();
                    await transactionalSession.Open(new AzureTableOpenSessionOptions(new TableEntityPartitionKey(ctx.TestRunId.ToString()), new TableInformation(SetupFixture.TableName)));

                    await transactionalSession.SendLocal(new SampleMessage());

                    var storageSession = transactionalSession.SynchronizedStorageSession.AzureTablePersistenceSession();

                    var entity = new MyTableEntity
                    {
                        RowKey = entityRowId,
                        PartitionKey = ctx.TestRunId.ToString(),
                        Data = "MyCustomData"
                    };

                    storageSession.Batch.Add(new TableTransactionAction(TableTransactionActionType.Add, entity));

                    await transactionalSession.Commit();
                }))
                .Done(c => c.MessageReceived)
                .Run();

            try
            {
                var entity = SetupFixture.TableClient.GetEntity<TableEntity>(context.TestRunId.ToString(), entityRowId).Value;
                Assert.That(entity.TryGetValue("Data", out var entityValue), Is.True);
                Assert.That(entityValue, Is.EqualTo("MyCustomData"));
            }
            catch (RequestFailedException e) when (e.Status == (int)HttpStatusCode.NotFound)
            {
                Assert.Fail("TableEntity does not exist");
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_send_messages_and_store_document_in_azuretable_session_on_transactional_session_commit(bool outboxEnabled)
        {
            var entityRowId = Guid.NewGuid().ToString();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<AnEndpoint>(s => s.When(async (_, ctx) =>
                {
                    using var scope = ctx.ServiceProvider.CreateScope();
                    using var transactionalSession = scope.ServiceProvider.GetRequiredService<ITransactionalSession>();
                    await transactionalSession.Open(new AzureTableOpenSessionOptions(new TableEntityPartitionKey(ctx.TestRunId.ToString()), new TableInformation(SetupFixture.TableName)));

                    await transactionalSession.SendLocal(new SampleMessage());

                    var storageSession = scope.ServiceProvider.GetRequiredService<IAzureTableStorageSession>();

                    var entity = new MyTableEntity
                    {
                        RowKey = entityRowId,
                        PartitionKey = ctx.TestRunId.ToString(),
                        Data = "MyCustomData"
                    };

                    storageSession.Batch.Add(new TableTransactionAction(TableTransactionActionType.Add, entity));

                    await transactionalSession.Commit();
                }))
                .Done(c => c.MessageReceived)
                .Run();

            try
            {
                var entity = SetupFixture.TableClient.GetEntity<TableEntity>(context.TestRunId.ToString(), entityRowId).Value;
                Assert.That(entity.TryGetValue("Data", out var entityValue), Is.True);
                Assert.That(entityValue, Is.EqualTo("MyCustomData"));
            }
            catch (RequestFailedException e) when (e.Status == (int)HttpStatusCode.NotFound)
            {
                Assert.Fail("TableEntity does not exist");
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_not_send_messages_if_session_is_not_committed(bool outboxEnabled)
        {
            var entityRowId = Guid.NewGuid().ToString();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<AnEndpoint>(s => s.When(async (statelessSession, ctx) =>
                {
                    using (var scope = ctx.ServiceProvider.CreateScope())
                    using (var transactionalSession = scope.ServiceProvider.GetRequiredService<ITransactionalSession>())
                    {
                        await transactionalSession.Open(new AzureTableOpenSessionOptions(new TableEntityPartitionKey(ctx.TestRunId.ToString()), new TableInformation(SetupFixture.TableName)));

                        await transactionalSession.SendLocal(new SampleMessage());

                        var storageSession = transactionalSession.SynchronizedStorageSession.AzureTablePersistenceSession();

                        var entity = new MyTableEntity
                        {
                            RowKey = entityRowId,
                            PartitionKey = ctx.TestRunId.ToString(),
                            Data = "MyCustomData"
                        };

                        storageSession.Batch.Add(new TableTransactionAction(TableTransactionActionType.Add, entity));
                    }

                    //Send immediately dispatched message to finish the test
                    await statelessSession.SendLocal(new CompleteTestMessage());
                }))
                .Done(c => c.CompleteMessageReceived)
                .Run();

            Assert.That(context.CompleteMessageReceived, Is.True);
            Assert.That(context.MessageReceived, Is.False);

            RequestFailedException requestFailedException = Assert.Throws<RequestFailedException>(() =>
            {
                SetupFixture.TableClient.GetEntity<TableEntity>(context.TestRunId.ToString(), entityRowId);
            });
            Assert.That(requestFailedException.Status, Is.EqualTo((int)HttpStatusCode.NotFound));
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_send_immediate_dispatch_messages_even_if_session_is_not_committed(bool outboxEnabled)
        {
            var result = await Scenario.Define<Context>()
                .WithEndpoint<AnEndpoint>(s => s.When(async (_, ctx) =>
                {
                    using var scope = ctx.ServiceProvider.CreateScope();
                    using var transactionalSession = scope.ServiceProvider.GetRequiredService<ITransactionalSession>();

                    await transactionalSession.Open(new AzureTableOpenSessionOptions(new TableEntityPartitionKey(ctx.TestRunId.ToString()), new TableInformation(SetupFixture.TableName)));

                    var sendOptions = new SendOptions();
                    sendOptions.RequireImmediateDispatch();
                    sendOptions.RouteToThisEndpoint();
                    await transactionalSession.Send(new SampleMessage(), sendOptions);
                }))
                .Done(c => c.MessageReceived)
                .Run();

            Assert.That(result.MessageReceived, Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_allow_using_synchronized_storage_even_when_there_are_no_outgoing_operations(bool outboxEnabled)
        {
            var entityRowId = Guid.NewGuid().ToString();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<AnEndpoint>(s => s.When(async (statelessSession, ctx) =>
                {
                    using (var scope = ctx.ServiceProvider.CreateScope())
                    using (var transactionalSession = scope.ServiceProvider.GetRequiredService<ITransactionalSession>())
                    {
                        await transactionalSession.Open(new AzureTableOpenSessionOptions(
                            new TableEntityPartitionKey(ctx.TestRunId.ToString()),
                            new TableInformation(SetupFixture.TableName)));

                        var storageSession =
                            transactionalSession.SynchronizedStorageSession.AzureTablePersistenceSession();

                        var entity = new MyTableEntity
                        {
                            RowKey = entityRowId,
                            PartitionKey = ctx.TestRunId.ToString(),
                            Data = "MyCustomData"
                        };

                        storageSession.Batch.Add(new TableTransactionAction(TableTransactionActionType.Add, entity));

                        // Deliberately not sending any messages via the transactional session before committing
                        await transactionalSession.Commit();
                    }

                    //Send immediately dispatched message to finish the test
                    await statelessSession.SendLocal(new CompleteTestMessage());
                }))
                .Done(c => c.CompleteMessageReceived)
                .Run();

            try
            {
                var entity = SetupFixture.TableClient.GetEntity<TableEntity>(context.TestRunId.ToString(), entityRowId).Value;
                Assert.That(entity.TryGetValue("Data", out var entityValue), Is.True);
                Assert.That(entityValue, Is.EqualTo("MyCustomData"));
            }
            catch (RequestFailedException e) when (e.Status == (int)HttpStatusCode.NotFound)
            {
                Assert.Fail("TableEntity does not exist");
            }
        }

        class Context : ScenarioContext, IInjectServiceProvider
        {
            public bool MessageReceived { get; set; }
            public bool CompleteMessageReceived { get; set; }
            public IServiceProvider ServiceProvider { get; set; }
        }

        class AnEndpoint : EndpointConfigurationBuilder
        {
            public AnEndpoint()
            {
                if ((bool)TestContext.CurrentContext.Test.Arguments[0]!)
                {
                    EndpointSetup<TransactionSessionDefaultServer>();
                }
                else
                {
                    EndpointSetup<TransactionSessionWithOutboxEndpoint>();
                }
            }

            class SampleHandler : IHandleMessages<SampleMessage>
            {
                public SampleHandler(Context testContext) => this.testContext = testContext;

                public Task Handle(SampleMessage message, IMessageHandlerContext context)
                {
                    testContext.MessageReceived = true;

                    return Task.CompletedTask;
                }

                readonly Context testContext;
            }

            class CompleteTestMessageHandler : IHandleMessages<CompleteTestMessage>
            {
                public CompleteTestMessageHandler(Context context) => testContext = context;

                public Task Handle(CompleteTestMessage message, IMessageHandlerContext context)
                {
                    testContext.CompleteMessageReceived = true;

                    return Task.CompletedTask;
                }

                readonly Context testContext;
            }
        }

        class SampleMessage : ICommand
        {
        }

        class CompleteTestMessage : ICommand
        {
        }

        public class MyTableEntity : ITableEntity
        {
            public string Data { get; set; }
            public string PartitionKey { get; set; }
            public string RowKey { get; set; }
            public DateTimeOffset? Timestamp { get; set; }
            public ETag ETag { get; set; }
        }
    }
}