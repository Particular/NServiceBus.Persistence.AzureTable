namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using NUnit.Framework;
    using SagaPersisters.AzureStoragePersistence;
    using ScenarioDescriptors;

    public class When_saga_has_duplicate_instances : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Dispatching_message_should_fail()
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureStoragePersistence.ConnectionString");
            var account = CloudStorageAccount.Parse(connectionString);
            var table = account.CreateCloudTableClient().GetTableReference(typeof(TwoInstanceSaga.TwoInstanceSagaState).Name);

            await table.CreateIfNotExistsAsync().ConfigureAwait(false);
            await ClearTable(table).ConfigureAwait(false);

            await Scenario.Define<Context>(c => c.OrderId = Guid.NewGuid().ToString())
                .WithEndpoint<ReceiverWithSagas>(b =>
                {
                    b.DoNotFailOnErrorMessages();
                    b.When((session, c) =>
                    {
                        var id1 = Guid.NewGuid();
                        var id2 = Guid.NewGuid();

                        c.SagasIds = new[]
                        {
                            id1,
                            id2
                        };

                        var e1 = CreateSagaEntityWithOrderId(id1, c.OrderId);
                        var e2 = CreateSagaEntityWithOrderId(id2, c.OrderId);

                        // insert sagas duplicates
                        table.Execute(TableOperation.Insert(e1));
                        table.Execute(TableOperation.Insert(e2));

                        return session.SendLocal(new Complete
                        {
                            OrderId = c.OrderId
                        });
                    });
                })
                .Done(c => c.FailedMessages.IsEmpty == false)
                .Repeat(r => r.For(Transports.Default))
                .Should(c =>
                {
                    CollectionAssert.IsNotEmpty(c.FailedMessages, "Should include at least one failed message.");

                    var failedMessages = c.FailedMessages.SelectMany(kvp => kvp.Value).ToArray();
                    foreach (var failedMessage in failedMessages)
                    {
                        Assert.IsInstanceOf<DuplicatedSagaFoundException>(failedMessage.Exception);

                        foreach (var sagasId in c.SagasIds)
                        {
                            Assert.True(failedMessage.Exception.Message.Contains(sagasId.ToString()));
                        }
                    }
                })
                .Run();
        }

        static TwoInstanceSaga.TwoInstanceSagaEntity CreateSagaEntityWithOrderId(Guid id, string orderId)
        {
            return new TwoInstanceSaga.TwoInstanceSagaEntity
            {
                PartitionKey = id.ToString(),
                RowKey = id.ToString(),
                OrderId = orderId,
                Id = id
            };
        }

        static async Task ClearTable(CloudTable table)
        {
            foreach (var entity in table.ExecuteQuery(new TableQuery()).ToArray())
            {
                await table.ExecuteAsync(TableOperation.Delete(entity)).ConfigureAwait(false);
            }
        }

        public class Context : ScenarioContext
        {
            public bool Completed { get; set; }
            public string OrderId { get; set; }
            public bool StartSagaMessageReceived { get; set; }
            public Guid[] SagasIds { get; set; }
        }

        class ReceiverWithSagas : EndpointConfigurationBuilder
        {
            public ReceiverWithSagas()
            {
                EndpointSetup<DefaultServer>(
                    config => { config.LimitMessageProcessingConcurrencyTo(1); });
            }
        }

        public class TwoInstanceSaga : Saga<TwoInstanceSaga.TwoInstanceSagaState>,
            IAmStartedByMessages<Start>,
            IHandleMessages<Complete>
        {
            public Context Context { get; set; }
            // ReSharper disable once MemberCanBePrivate.Global
            public Task Handle(Start message, IMessageHandlerContext context)
            {
                Data.OrderId = message.OrderId;
                Context.StartSagaMessageReceived = true;

                return Task.FromResult(0);
            }

            public Task Handle(Complete message, IMessageHandlerContext context)
            {
                throw new InvalidOperationException("Shouldn't have receive it.");
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TwoInstanceSagaState> mapper)
            {
                mapper.ConfigureMapping<Complete>(m => m.OrderId)
                    .ToSaga(s => s.OrderId);
                mapper.ConfigureMapping<Start>(m => m.OrderId)
                    .ToSaga(s => s.OrderId);
            }

            public class TwoInstanceSagaState : IContainSagaData
            {
                public virtual string OrderId { get; set; }

                public virtual Guid Id { get; set; }
                public virtual string Originator { get; set; }
                public virtual string OriginalMessageId { get; set; }
            }

            public class TwoInstanceSagaEntity : TableEntity
            {
                public virtual string OrderId { get; set; }
                public virtual Guid Id { get; set; }
                public virtual string Originator { get; set; }
                public virtual string OriginalMessageId { get; set; }
            }
        }

        [Serializable]
        public class Start : ICommand
        {
            public string OrderId { get; set; }
        }

        [Serializable]
        public class Complete : ICommand
        {
            public string OrderId { get; set; }
        }
    }
}