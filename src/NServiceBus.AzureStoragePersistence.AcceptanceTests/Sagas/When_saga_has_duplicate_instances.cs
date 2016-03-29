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
    using NServiceBus.SagaPersisters.Azure;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    public class When_saga_has_duplicate_instances : NServiceBusAcceptanceTest
    {
        [Test]
        public void Dispatching_message_should_fail()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var orderId = Guid.NewGuid().ToString();

            var connectionString = Environment.GetEnvironmentVariable("AzureStoragePersistence.ConnectionString");
            var account = CloudStorageAccount.Parse(connectionString);
            var table = account.CreateCloudTableClient().GetTableReference(typeof(TwoInstanceSaga.TwoInstanceSagaState).Name);

            table.CreateIfNotExists();

            ClearTable(table);

            table.Execute(TableOperation.Insert(CreateState(id1, orderId)));
            table.Execute(TableOperation.Insert(CreateState(id2, orderId)));

            var ex = Assert.Throws<AggregateException>(async () =>
                await Scenario.Define<Context>()
                    .WithEndpoint<ReceiverWithSagas>(b => b.When(
                        session => session.SendLocal(new Complete
                        {
                            OrderId = orderId
                        })))
                    //.AllowExceptions(ex => ex.Message.Contains(typeof(DuplicatedSagaFoundException).Name) || ex.Message.Contains(ReceiverWithSagas.ThrowOnSagaNotFound.Message))
                    .Run(TimeSpan.FromMinutes(5)));

            Assert.IsInstanceOf<DuplicatedSagaFoundException>(ex.InnerExceptions.Single());
        }

        private void ClearTable(CloudTable table)
        {
            foreach (var entity in table.ExecuteQuery(new TableQuery()).ToArray())
            {
                table.Execute(TableOperation.Delete(entity));
            }
        }

        private static TwoInstanceSaga.StateEntity CreateState(Guid id, string correlatingId)
        {
            var partitionKey = id.ToString();

            return new TwoInstanceSaga.StateEntity
            {
                Id = id,
                PartitionKey = partitionKey,
                RowKey = partitionKey,
                OriginalMessageId = null,
                Originator = "test",
                Timestamp = DateTimeOffset.Now.AddSeconds(-30),
                OrderId = correlatingId
            };
        }

        public class Context : ScenarioContext
        {
            private long _completes;
            private long _starts;
            public bool Completed { get; set; }
            public string OrderId { get; set; }
            public long StartsMessages => _starts;
            public long CompleteMessages => _completes;

            public void RegisterStart()
            {
                Interlocked.Increment(ref _starts);
            }

            public void RegisterComplete()
            {
                Interlocked.Increment(ref _completes);
            }
        }

        private class ReceiverWithSagas : EndpointConfigurationBuilder
        {
            public ReceiverWithSagas()
            {
                EndpointSetup<DefaultServer>(
                    config => { });
            }

            public class ThrowOnSagaNotFound : IHandleSagaNotFound
            {
                public const string Message = "Moving non existent saga message to the error queue";

                public Task Handle(object message, IMessageProcessingContext context)
                {
                    throw new Exception(Message);
                }
            }
        }

        public class TwoInstanceSaga : Saga<TwoInstanceSaga.TwoInstanceSagaState>,
            IAmStartedByMessages<Start>,
            IHandleMessages<Complete>
        {
            // ReSharper disable once MemberCanBePrivate.Global
            public Context Context { get; set; }

            public Task Handle(Start message, IMessageHandlerContext context)
            {
                Context.RegisterStart();
                Data.OrderId = message.OrderId;

                return Task.FromResult(0);
            }

            public Task Handle(Complete message, IMessageHandlerContext context)
            {
                Context.RegisterStart();
                MarkAsComplete();
                if (message.OrderId == Context.OrderId)
                {
                    Context.Completed = true;
                }

                return Task.FromResult(0);
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TwoInstanceSagaState> mapper)
            {
                mapper.ConfigureMapping<Complete>(m => m.OrderId)
                    .ToSaga(s => s.OrderId);
                mapper.ConfigureMapping<Start>(m => m.OrderId)
                    .ToSaga(s => s.OrderId);
            }

            private interface IState : IContainSagaData
            {
                // ReSharper disable once UnusedMemberInSuper.Global
                string OrderId { get; set; }
            }

            public class TwoInstanceSagaState : IState
            {
                public virtual string OrderId { get; set; }

                public virtual Guid Id { get; set; }
                public virtual string Originator { get; set; }
                public virtual string OriginalMessageId { get; set; }
            }

            public class StateEntity : TableEntity, IState
            {
                public Guid Id { get; set; }
                public string Originator { get; set; }
                public string OriginalMessageId { get; set; }
                public string OrderId { get; set; }
            }
        }

        public class Start : ICommand
        {
            public string OrderId { get; set; }
        }

        public class Complete : ICommand
        {
            public string OrderId { get; set; }
        }
    }
}