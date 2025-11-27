namespace NServiceBus.AcceptanceTests;

using System;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;
using System.Net;
using Azure;
using Azure.Data.Tables;

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

        var sagaEntity = await GetEntity(context.SagaId);

        Assert.Multiple(() =>
        {
            Assert.That(sagaEntity.TryGetValue(nameof(EndpointWithSagaWithComplexState.ComplexStateSagaData.NullableDouble), out var nullableDouble), Is.True);
            Assert.That(nullableDouble, Is.EqualTo(4.5d));

            Assert.That(sagaEntity.TryGetValue(nameof(EndpointWithSagaWithComplexState.ComplexStateSagaData.IntArray), out var intArray), Is.True);
            Assert.That(intArray, Is.EqualTo("[1,2,3,4]"));

            Assert.That(sagaEntity.TryGetValue(nameof(EndpointWithSagaWithComplexState.ComplexStateSagaData.ComplexData), out var complexData), Is.True);
            Assert.That(complexData, Is.EqualTo("{\"Data\":\"SomeData\"}"));

            Assert.That(sagaEntity.TryGetValue(nameof(EndpointWithSagaWithComplexState.ComplexStateSagaData.NullableBool), out var nullableBool), Is.True);
            Assert.That(nullableBool, Is.EqualTo(true));

            Assert.That(sagaEntity.TryGetValue(nameof(EndpointWithSagaWithComplexState.ComplexStateSagaData.NullableGuid), out var nullableGuid), Is.True);
            Assert.That(nullableGuid, Is.EqualTo(new Guid("3C623C1F-80AB-4036-86CA-C2020FAE2EFE")));

            Assert.That(sagaEntity.TryGetValue(nameof(EndpointWithSagaWithComplexState.ComplexStateSagaData.NullableLong), out var nullableLong), Is.True);
            Assert.That(nullableLong, Is.EqualTo(10));

            Assert.That(sagaEntity.TryGetValue(nameof(EndpointWithSagaWithComplexState.ComplexStateSagaData.NullableInt), out var nullableInt), Is.True);
            Assert.That(nullableInt, Is.EqualTo(10));

            Assert.That(sagaEntity.TryGetValue(nameof(EndpointWithSagaWithComplexState.ComplexStateSagaData.ByteArray), out var byteArray), Is.True);
            Assert.That(byteArray, Is.EqualTo(new byte[] { 1 }));

            Assert.That(sagaEntity.ContainsKey("NServiceBus_2ndIndexKey"), Is.False, "Entity should not contain secondary index property");
        });
    }

    static async Task<TableEntity> GetEntity(Guid sagaId)
    {
        var table = SetupFixture.TableClient;

        // table scan but still probably the easiest way to do it, otherwise we would have to take the partition key into account which complicates things because this test is shared
        //var query = new TableQuery<DynamicTableEntity>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, sagaId.ToString()));

        try
        {
            var tableEntity = await table.QueryAsync<TableEntity>(entity => entity.RowKey == sagaId.ToString()).FirstOrDefaultAsync();
            return tableEntity;
        }
        catch (RequestFailedException e)
        {
            if (e.Status == (int)HttpStatusCode.NotFound)
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
        public EndpointWithSagaWithComplexState() => EndpointSetup<DefaultServer>();

        public class SagaWithComplexState(Context testContext) : Saga<ComplexStateSagaData>, IAmStartedByMessages<StartSagaMessage>, IHandleMessages<ContinueSagaMessage>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
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

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ComplexStateSagaData> mapper) =>
                mapper.MapSaga(s => s.SomeId)
                    .ToMessage<StartSagaMessage>(m => m.SomeId)
                    .ToMessage<ContinueSagaMessage>(m => m.SomeId);
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