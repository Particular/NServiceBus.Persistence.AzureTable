namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using System.Net;
    using Microsoft.Azure.Cosmos.Table;

    public partial class When_saga_with_complex_state : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_work()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithSagaWithComplexState>(b => b.When(session =>
                    session.SendLocal(new StartSagaMessage
                    {
                        SomeId = Guid.NewGuid()
                    })))
                .Done(c => c.Done)
                .Run();

            var sagaEntity = GetEntity(context.SagaId);

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

            Assert.IsFalse(sagaEntity.Properties.ContainsKey("NServiceBus_2ndIndexKey"), "Entity should not contain secondary index property");
        }

        private static DynamicTableEntity GetEntity(Guid sagaId)
        {
            var table = SetupFixture.Table;

            // table scan but still probably the easiest way to do it, otherwise we would have to take the partition key into account which complicates things because this test is shared
            var query = new TableQuery<DynamicTableEntity>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, sagaId.ToString()));

            try
            {
                var tableEntity = table.ExecuteQuery(query).FirstOrDefault();
                return tableEntity;
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    return null;
                }

                throw;
            }
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
                    Data.IntArray = new[] {1, 2, 3, 4};
                    Data.NullableDouble = 4.5d;
                    Data.ByteArray = new byte[] {1};
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
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                    mapper.ConfigureMapping<ContinueSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }

                private readonly Context testContext;
            }

            public class ComplexStateSagaData : IContainSagaData
            {
                public Guid Id { get; set; }
                public string Originator { get; set; }
                public string OriginalMessageId { get; set; }
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