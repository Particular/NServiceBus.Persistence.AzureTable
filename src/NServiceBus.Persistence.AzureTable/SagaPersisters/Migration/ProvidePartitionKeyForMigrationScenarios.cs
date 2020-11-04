namespace NServiceBus.Persistence.AzureTable.Migration
{
    using System.Threading.Tasks;
    using Pipeline;
    using Sagas;
    using Microsoft.Azure.Cosmos.Table;

    class ProvidePartitionKeyForMigrationScenarios : IProvidePartitionKeyForMigrationScenarios
    {
        public ProvidePartitionKeyForMigrationScenarios(IProvideCloudTableClient tableClientProvider, TableHolderResolver resolver, SecondaryIndex secondaryIndices, bool migrationModeEnabled)
        {
            this.migrationModeEnabled = migrationModeEnabled;
            this.resolver = resolver;
            this.secondaryIndices = secondaryIndices;
            client = tableClientProvider.Client;
        }

        public async Task SetPartitionKey<TSagaData>(IIncomingLogicalMessageContext context,
            SagaCorrelationProperty correlationProperty)
            where TSagaData : IContainSagaData
        {
            if (context.Extensions.TryGet<TableEntityPartitionKey>(out _))
            {
                return;
            }

            if (context.Headers.TryGetValue(Headers.SagaId, out var sagaId))
            {
                context.Extensions.Set(new TableEntityPartitionKey(sagaId));
                return;
            }

            if (migrationModeEnabled)
            {
                var tableHolder = resolver.ResolveAndSetIfAvailable(context.Extensions);
                // slight duplication between saga persister and here when it comes to conventional tables
                // TODO: Is it ok to assume tables don't need to be created?
                var sagaTable = tableHolder == null ? client.GetTableReference(typeof(TSagaData).Name) : tableHolder.Table;

                var nullableSagaId = await secondaryIndices.FindSagaId<TSagaData>(sagaTable, correlationProperty.Name, correlationProperty.Value)
                    .ConfigureAwait(false);

                if (nullableSagaId.HasValue)
                {
                    context.Extensions.Set(new TableEntityPartitionKey(nullableSagaId.Value.ToString()));
                    return;
                }
            }

            var deterministicSagaId = SagaIdGenerator.Generate(typeof(TSagaData), correlationProperty.Name, correlationProperty.Value);
            context.Extensions.Set(new TableEntityPartitionKey(deterministicSagaId.ToString()));
        }

        private readonly SecondaryIndex secondaryIndices;
        private CloudTableClient client;
        private readonly TableHolderResolver resolver;
        private readonly bool migrationModeEnabled;
    }
}