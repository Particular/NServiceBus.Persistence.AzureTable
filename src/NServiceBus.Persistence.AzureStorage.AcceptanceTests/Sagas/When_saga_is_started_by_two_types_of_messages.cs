namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_saga_is_started_by_two_types_of_messages : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Every_saga_should_be_started_once_and_updated_by_second_message()
        {
            const int expectedNumberOfCreatedSagas = 20;

            var guids = new HashSet<string>(Enumerable.Repeat(1, expectedNumberOfCreatedSagas).Select(i => Guid.NewGuid().ToString())).OrderBy(s => s);

            var context = await Scenario.Define<Context>()
                .WithEndpoint<ReceiverWithSagas>(b => b.When(async session =>
                {
                    foreach (var guid in guids)
                    {
                        await session.SendLocal(new OrderBilled
                        {
                            OrderId = guid
                        });
                        await session.SendLocal(new OrderPlaced
                        {
                            OrderId = guid
                        });
                    }
                }).DoNotFailOnErrorMessages())
                .Done(c => c.CompletedIds.OrderBy(s => s).ToArray().Intersect(guids).Count() == expectedNumberOfCreatedSagas)
                .Run(TimeSpan.FromSeconds(60)).ConfigureAwait(false);

            CollectionAssert.AreEquivalent(guids, context.CompletedIds.OrderBy(s => s).ToArray());
        }

        public class Context : ScenarioContext
        {
            public int CompletedIdsCount => completed.Count;
            public IEnumerable<string> CompletedIds => completed.Keys;

            public void MarkAsCompleted(string orderId)
            {
                completed.AddOrUpdate(orderId, orderId, (o1, o2) => o1);
            }

            ConcurrentDictionary<string, string> completed = new ConcurrentDictionary<string, string>();
        }

        public class ReceiverWithSagas : EndpointConfigurationBuilder
        {
            public ReceiverWithSagas()
            {
                EndpointSetup<DefaultServer>(
                    config =>
                    {
                        config.LimitMessageProcessingConcurrencyTo(3);
                        config.Recoverability().CustomPolicy((rc, er) => RecoverabilityAction.ImmediateRetry());
                    });
            }
        }

        public class ShippingPolicy : Saga<ShippingPolicy.State>,
            IAmStartedByMessages<OrderPlaced>,
            IAmStartedByMessages<OrderBilled>
        {
            public Context Context { get; set; }

            public Task Handle(OrderBilled message, IMessageHandlerContext context)
            {
                Data.OrderId = message.OrderId;
                Data.Billed = true;

                TryComplete();

                return Task.FromResult(0);
            }

            public Task Handle(OrderPlaced message, IMessageHandlerContext context)
            {
                Data.OrderId = message.OrderId;
                Data.Placed = true;

                TryComplete();
                return Task.FromResult(0);
            }

            void TryComplete()
            {
                if (Data.Billed && Data.Placed)
                {
                    MarkAsComplete();
                    Context.MarkAsCompleted(Data.OrderId);
                }
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<State> mapper)
            {
                mapper.ConfigureMapping<OrderPlaced>(m => m.OrderId)
                    .ToSaga(s => s.OrderId);
                mapper.ConfigureMapping<OrderBilled>(m => m.OrderId)
                    .ToSaga(s => s.OrderId);
            }

            public class State : IContainSagaData
            {
                public virtual string OrderId { get; set; }

                public virtual bool Placed { get; set; }
                public virtual bool Billed { get; set; }
                public virtual Guid Id { get; set; }
                public virtual string Originator { get; set; }
                public virtual string OriginalMessageId { get; set; }
            }
        }

        public class OrderBilled : ICommand
        {
            public string OrderId { get; set; }
        }

        public class OrderPlaced : ICommand
        {
            public string OrderId { get; set; }
        }
    }
}