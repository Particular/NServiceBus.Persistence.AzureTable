namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using AcceptanceTesting;
    using Microsoft.Azure.Cosmos.Table;
    using NUnit.Framework;
    using Persistence.AzureTable;
    using System.Linq;

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

                    storageSession.Batch.Add(TableOperation.Insert(entity));

                    await transactionalSession.Commit().ConfigureAwait(false);
                }))
                .Done(c => c.MessageReceived)
                .Run();

            var query = new TableQuery<DynamicTableEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, context.TestRunId.ToString()))
                .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, entityRowId));

            var tableEntity = SetupFixture.Table.ExecuteQuery(query).FirstOrDefault();

            Assert.IsNotNull(tableEntity);
            Assert.AreEqual(tableEntity.Properties["Data"].StringValue, "MyCustomData");
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

                    storageSession.Batch.Add(TableOperation.Insert(entity));

                    await transactionalSession.Commit(CancellationToken.None).ConfigureAwait(false);
                }))
                .Done(c => c.MessageReceived)
                .Run();

            var query = new TableQuery<DynamicTableEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, context.TestRunId.ToString()))
                .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, entityRowId));

            var tableEntity = SetupFixture.Table.ExecuteQuery(query).FirstOrDefault();

            Assert.IsNotNull(tableEntity);
            Assert.AreEqual(tableEntity.Properties["Data"].StringValue, "MyCustomData");
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

                        storageSession.Batch.Add(TableOperation.Insert(entity));
                    }

                    //Send immediately dispatched message to finish the test
                    await statelessSession.SendLocal(new CompleteTestMessage());
                }))
                .Done(c => c.CompleteMessageReceived)
                .Run();

            Assert.True(context.CompleteMessageReceived);
            Assert.False(context.MessageReceived);

            var query = new TableQuery<DynamicTableEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, context.TestRunId.ToString()))
                .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, entityRowId));
            Assert.IsEmpty(SetupFixture.Table.ExecuteQuery(query));
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

            Assert.True(result.MessageReceived);
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

        public class MyTableEntity : TableEntity
        {
            public string Data { get; set; }
        }
    }
}