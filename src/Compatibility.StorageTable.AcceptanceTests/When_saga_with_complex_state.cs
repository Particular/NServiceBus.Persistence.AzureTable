namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using Microsoft.Azure.Cosmos.Table;
    using Extensibility;
    using Sagas;

    // Verifies that even with complex data our entity conversion stays compatible
    public class When_saga_with_complex_state : CompatibilityAcceptanceTest
    {
        [Test]
        public async Task Should_work()
        {
            Requires.AzureTables();

            var correlationPropertyValue = Guid.NewGuid();
            var sagaId = Guid.NewGuid();

            var previousSagaData = new EndpointWithSagaWithComplexState.ComplexStateSagaData
            {
                Id = sagaId,
                OriginalMessageId = "",
                Originator = "",
                SomeId = correlationPropertyValue,
                IntArray = new[] { 1, 2, 3, 4 },
                NullableDouble = 4.5d,
                ByteArray = new byte[] { 1 },
                NullableBool = true,
                NullableGuid = new Guid("3C623C1F-80AB-4036-86CA-C2020FAE2EFE"),
                NullableLong = 10,
                NullableInt = 10,
                ComplexData = new EndpointWithSagaWithComplexState.SomethingComplex
                {
                    Data = "SomeData"
                }
            };

            var sagaCorrelationProperty = new SagaCorrelationProperty("SomeId", correlationPropertyValue);
            await PersisterUsingSecondaryIndexes.Save(previousSagaData, sagaCorrelationProperty, null, new ContextBag());

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithSagaWithComplexState>(b => b.When(session =>
                    session.SendLocal(new ContinueSagaMessage
                    {
                        SomeId = correlationPropertyValue
                    })))
                .Done(c => c.Done)
                .Run();

            var sagaEntity = GetByRowKey<EndpointWithSagaWithComplexState.ComplexStateSagaData>(context.SagaId.ToString());

            var nullableDoubleProp = sagaEntity[nameof(EndpointWithSagaWithComplexState.ComplexStateSagaData.NullableDouble)];
            var intArrayProp = sagaEntity[nameof(EndpointWithSagaWithComplexState.ComplexStateSagaData.IntArray)];
            var complexObjectProp = sagaEntity[nameof(EndpointWithSagaWithComplexState.ComplexStateSagaData.ComplexData)];
            var nullableBoolProp = sagaEntity[nameof(EndpointWithSagaWithComplexState.ComplexStateSagaData.NullableBool)];
            var nullableGuidProp = sagaEntity[nameof(EndpointWithSagaWithComplexState.ComplexStateSagaData.NullableGuid)];
            var nullableLongProp = sagaEntity[nameof(EndpointWithSagaWithComplexState.ComplexStateSagaData.NullableLong)];
            var nullableIntProp = sagaEntity[nameof(EndpointWithSagaWithComplexState.ComplexStateSagaData.NullableInt)];
            var byteArrayProp = sagaEntity[nameof(EndpointWithSagaWithComplexState.ComplexStateSagaData.ByteArray)];

            Assert.AreEqual(EdmType.Double, nullableDoubleProp.PropertyType);
            Assert.AreEqual(4.5d, nullableDoubleProp.DoubleValue);

            Assert.AreEqual(EdmType.String, intArrayProp.PropertyType);
            Assert.AreEqual("[1,2,3,4]", intArrayProp.StringValue);

            Assert.AreEqual(EdmType.String, complexObjectProp.PropertyType);
            Assert.AreEqual("{\"Data\":\"SomeData\"}", complexObjectProp.StringValue);

            Assert.AreEqual(EdmType.Boolean, nullableBoolProp.PropertyType);
            Assert.AreEqual(true, nullableBoolProp.BooleanValue);

            Assert.AreEqual(EdmType.Guid, nullableGuidProp.PropertyType);
            Assert.AreEqual(new Guid("3C623C1F-80AB-4036-86CA-C2020FAE2EFE"), nullableGuidProp.GuidValue);

            Assert.AreEqual(EdmType.Int64, nullableLongProp.PropertyType);
            Assert.AreEqual(10, nullableLongProp.Int64Value);

            Assert.AreEqual(EdmType.Int32, nullableIntProp.PropertyType);
            Assert.AreEqual(10, nullableIntProp.Int32Value);

            Assert.AreEqual(EdmType.Binary, byteArrayProp.PropertyType);
            CollectionAssert.AreEqual(new byte[] { 1 }, byteArrayProp.BinaryValue);

            Assert.AreEqual(sagaId, context.SagaId);
        }

        public class Context : ScenarioContext
        {
            public bool Done { get; set; }
            public Guid SagaId { get; set; }
        }

        public class EndpointWithSagaWithComplexState : EndpointConfigurationBuilder
        {
            public EndpointWithSagaWithComplexState()
            {
                EndpointSetup<DefaultServer>();
            }

            public class SagaWithComplexState : Saga<ComplexStateSagaData>, IAmStartedByMessages<StartSagaMessage>, IHandleMessages<ContinueSagaMessage>
            {
                public SagaWithComplexState(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    // would have been called this way on older persistence
                    Data.IntArray = new[] { 1, 2, 3, 4 };
                    Data.NullableDouble = 4.5d;
                    Data.ByteArray = new byte[] { 1 };
                    Data.NullableBool = true;
                    Data.NullableGuid = new Guid("3C623C1F-80AB-4036-86CA-C2020FAE2EFE");
                    Data.NullableLong = 10;
                    Data.NullableInt = 10;
                    Data.ComplexData = new SomethingComplex
                    {
                        Data = "SomeData"
                    };

                    return context.SendLocal(new ContinueSagaMessage { SomeId = message.SomeId });
                }

                public Task Handle(ContinueSagaMessage message, IMessageHandlerContext context)
                {
                    testContext.SagaId = Data.Id;
                    testContext.Done = true;
                    return Task.CompletedTask;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ComplexStateSagaData> mapper)
                {
                    mapper.MapSaga(s => s.SomeId)
                        .ToMessage<StartSagaMessage>(m => m.SomeId)
                        .ToMessage<ContinueSagaMessage>(m => m.SomeId);
                }

                readonly Context testContext;
            }

            public class ComplexStateSagaData : ContainSagaData
            {
                public Guid SomeId { get; set; }

                public int[] IntArray { get; set; }
                public double? NullableDouble { get; set; }

                public bool? NullableBool { get; set; }
                public int? NullableInt { get; set; }
                public Guid? NullableGuid { get; set; }
                public long? NullableLong { get; set; }
                public byte[] ByteArray { get; set; }

                public SomethingComplex ComplexData { get; set; }
            }

            public class SomethingComplex
            {
                public string Data { get; set; }
            }
        }

        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }

        public class ContinueSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }
    }
}