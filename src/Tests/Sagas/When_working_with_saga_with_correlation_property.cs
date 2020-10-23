#if NETFRAMEWORK
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
        const bool AssumeSecondaryIndicesExist = true;

        [Test]
        public async Task Should_not_issue_table_scan()
        {
            var connectionString = Testing.Utilities.GetEnvConfiguredConnectionStringForPersistence();
            AzureSagaPersister createSagaPersister() => new AzureSagaPersister(new CloudTableClientFromConnectionString(connectionString), true, AssumeSecondaryIndicesExist);

            // warm up table cache
            var warmUp = new AzureSagaPersister(new CloudTableClientFromConnectionString(connectionString), true, false);
            await warmUp.Get<SagaData>(Guid.NewGuid(), null, new ContextBag()).ConfigureAwait(false);

            using (var recorder = new AzureRequestRecorder())
            {
                // try to correlate first
                {
                    var persister = createSagaPersister();

                    await persister.Get<SagaData>(nameof(SagaData.Correlation), CorrelationValue, null, new ContextBag()).ConfigureAwait(false);
                }

                // save saga
                {
                    var saga = new SagaData
                    {
                        Id = Guid.NewGuid(),
                        Originator = "Moo",
                        OriginalMessageId = "MooId",
                        Correlation = CorrelationValue
                    };

                    var persister = createSagaPersister();
                    await persister.Save(saga, new SagaCorrelationProperty(nameof(SagaData.Correlation), CorrelationValue), null, new ContextBag()).ConfigureAwait(false);
                }

                // get by correlation and update
                {
                    var persister = createSagaPersister();
                    var ctx = new ContextBag();

                    var saga = await persister.Get<SagaData>(nameof(SagaData.Correlation), CorrelationValue, null, ctx).ConfigureAwait(false);
                    saga.Value = UpdatedValue;

                    await persister.Update(saga, null, ctx).ConfigureAwait(false);
                }

                // get by correlation and complete
                {
                    var persister = createSagaPersister();
                    var ctx = new ContextBag();

                    var saga = await persister.Get<SagaData>(nameof(SagaData.Correlation), CorrelationValue, null, ctx).ConfigureAwait(false);
                    await persister.Complete(saga, null, ctx).ConfigureAwait(false);
                }

                recorder.Print(Console.Out);

                var gets = recorder.Requests.Where(r => r.ToLower().Contains("get"));
                var getsWithNoPartitionKey = gets.Where(get =>
                    get.Contains("PartitionKey%20eq") == false &&
                    get.Contains("PartitionKey=") == false).ToArray();

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
#endif