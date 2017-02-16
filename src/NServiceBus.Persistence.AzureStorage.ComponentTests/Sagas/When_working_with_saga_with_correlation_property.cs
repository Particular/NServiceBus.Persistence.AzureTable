namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Persisters
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    public class When_working_with_saga_with_correlation_property
    {
        const string CorrelationValue = "correlation-value";
        const string UpdatedValue = "updated-value";

        [Test]
        public async Task Should_not_issue_table_scan()
        {
            var connectionString = AzurePersistenceTests.GetConnectionString();

            // warm up table cache
            var warmUp = new AzureSagaPersister(connectionString, true);
            await warmUp.Get<SagaData>(Guid.NewGuid(), null, new ContextBag()).ConfigureAwait(false);

            using (var recorder = new AzureRequestRecorder())
            {
                // save saga
                {
                    var saga = new SagaData
                    {
                        Id = Guid.NewGuid(),
                        Originator = "Moo",
                        OriginalMessageId = "MooId",
                        Correlation = CorrelationValue
                    };

                    var persister = new AzureSagaPersister(connectionString, true);
                    await persister.Save(saga, new SagaCorrelationProperty(nameof(SagaData.Correlation), CorrelationValue), null, new ContextBag()).ConfigureAwait(false);
                }

                // get by correlation and update
                {
                    var persister = new AzureSagaPersister(connectionString, true);
                    var ctx = new ContextBag();

                    var saga = await persister.Get<SagaData>(nameof(SagaData.Correlation), CorrelationValue, null, ctx).ConfigureAwait(false);
                    saga.Value = UpdatedValue;

                    await persister.Update(saga, null, ctx).ConfigureAwait(false);
                }

                // get by correlation and complete
                {
                    var persister = new AzureSagaPersister(connectionString, true);
                    var ctx = new ContextBag();

                    var saga = await persister.Get<SagaData>(nameof(SagaData.Correlation), CorrelationValue, null, ctx).ConfigureAwait(false);
                    await persister.Complete(saga, null, ctx).ConfigureAwait(false);
                }

                recorder.Print(Console.Out);

                var gets = recorder.Requests.Where(r => r.ToLower().Contains("get"));
                var getsWithNoPartitionKey = gets.Where(get => get.Contains("PartitionKey") == false).ToArray();

                // only asking for a table
                CollectionAssert.IsEmpty(getsWithNoPartitionKey);
            }
        }
    }

    class SagaData : ContainSagaData
    {
        public string Value { get; set; }
        public string Correlation { get; set; }
    }
}