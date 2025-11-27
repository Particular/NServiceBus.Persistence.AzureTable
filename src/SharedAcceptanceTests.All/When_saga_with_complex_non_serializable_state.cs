namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using global::Newtonsoft.Json;
    using global::Newtonsoft.Json.Serialization;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public partial class When_saga_with_complex_non_serializable_state : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw_not_supported()
        {
            var exception = Assert.ThrowsAsync<MessageFailedException>(async () =>
                await Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithNonSerializableSaga>(b => b.When(session => session.SendLocal(new StartSagaMessage
                    {
                        SomeId = Guid.NewGuid()
                    })))
                    .Done(c => c.FailedMessages.Any())
                    .Run());

            Assert.That(exception.ScenarioContext.FailedMessages, Has.Count.EqualTo(1));
            Assert.That(
                exception.FailedMessage.Exception.Message,
                Does.Contain("The property type 'SomethingComplex' is not supported in Azure Table Storage and it cannot be serialized with JSON.NET."));
        }

        public class Context : ScenarioContext
        {
        }

        public class EndpointWithNonSerializableSaga : EndpointConfigurationBuilder
        {
            public EndpointWithNonSerializableSaga() =>
                EndpointSetup<DefaultServer>(b =>
                {
                    var sagaPersistence = b.UsePersistence<AzureTablePersistence, StorageType.Sagas>();
                    var customSettings = new JsonSerializerSettings { ContractResolver = new NonAbstractDefaultContractResolver() };
                    sagaPersistence.JsonSettings(customSettings);
                });

            class NonAbstractDefaultContractResolver : DefaultContractResolver
            {
                protected override JsonObjectContract CreateObjectContract(Type objectType)
                {
                    if (objectType.IsAbstract || objectType.IsInterface)
                    {
                        throw new ArgumentException("Cannot serialize an abstract class/interface", nameof(objectType));
                    }
                    return base.CreateObjectContract(objectType);
                }
            }

            public class NonSerializableSaga : Saga<NonSerializableSagaData>, IAmStartedByMessages<StartSagaMessage>
            {
                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    Data.NonserializableValue = new SomethingComplex();
                    return Task.CompletedTask;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<NonSerializableSagaData> mapper) =>
                    mapper.MapSaga(s => s.SomeId)
                        .ToMessage<StartSagaMessage>(m => m.SomeId);
            }

            public class NonSerializableSagaData : ContainSagaData
            {
                public Guid SomeId { get; set; }
                public SomethingComplex NonserializableValue { get; set; }
            }

            public class SomethingComplex
            {
                public IDisposable Disposable { get; set; }
            }
        }

        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }
    }
}