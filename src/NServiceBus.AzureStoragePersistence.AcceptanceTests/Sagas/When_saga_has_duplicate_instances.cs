namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.SagaPersisters.Azure;
    using NUnit.Framework;

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

                    b.When((session, c) => session.SendLocal(new Start
                    {
                        OrderId = c.OrderId
                    }));

                    b.When(c => c.StartSagaMessageReceived, (session, c) =>
                    {
                        var wait = new SpinWait();
                        DynamicTableEntity[] entries;
                        do
                        {
                            wait.SpinOnce();
                            entries = table.ExecuteQuery(new TableQuery()).ToArray();
                        } while (entries.Length < 2);

                        // select saga row
                        var id = new Guid();
                        var saga = entries.First(dte => Guid.TryParse(dte.PartitionKey, out id));

                        // copy saga
                        var newId = Guid.NewGuid();
                        saga.PartitionKey = newId.ToString();
                        saga.RowKey = newId.ToString();
                        saga.ETag = null;
                        saga.Properties["Id"].GuidValue = newId;
                        table.Execute(TableOperation.Insert(saga));

                        c.SagasIds = new[]
                        {
                            id,
                            newId
                        };

                        // delete the index entry making a real duplicate
                        var indexEntry = entries.First(dte => ReferenceEquals(dte, saga) == false);
                        table.Execute(TableOperation.Delete(indexEntry));

                        return session.SendLocal(new Complete
                        {
                            OrderId = c.OrderId
                        }).ContinueWith(t => session.SendLocal(new FinalMessage()));
                    });
                })
                .Done(c => c.FinalMessageReceived)
                .Repeat(r => r.For(Transports.Default))
                .Should(c =>
                {
                    var failedMessage = c.FailedMessages.SelectMany(kvp => kvp.Value).Single();
                    Assert.IsInstanceOf<DuplicatedSagaFoundException>(failedMessage.Exception);

                    foreach (var sagasId in c.SagasIds)
                    {
                        Assert.True(failedMessage.Exception.Message.Contains(sagasId.ToString()));
                    }
                })
                .Run();
        }

        private static async Task ClearTable(CloudTable table)
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
            public bool FinalMessageReceived { get; set; }
            public Guid[] SagasIds { get; set; }
        }

        private class ReceiverWithSagas : EndpointConfigurationBuilder
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
        }

        public class FinalMessageHandler : IHandleMessages<FinalMessage>
        {
            public Context Context { get; set; }

            public Task Handle(FinalMessage message, IMessageHandlerContext context)
            {
                Context.FinalMessageReceived = true;
                return Task.FromResult(0);
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

        [Serializable]
        public class FinalMessage : ICommand
        {
        }
    }
}