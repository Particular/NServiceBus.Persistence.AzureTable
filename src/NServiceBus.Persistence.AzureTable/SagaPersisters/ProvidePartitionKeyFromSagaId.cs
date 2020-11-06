namespace NServiceBus.Persistence.AzureTable.Migration
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Sagas;
    using Microsoft.Azure.Cosmos.Table;

    class ProvidePartitionKeyFromSagaId : IProvidePartitionKeyFromSagaId
    {
        public ProvidePartitionKeyFromSagaId(IProvideCloudTableClient tableClientProvider, TableHolderResolver resolver, SecondaryIndex secondaryIndices, bool migrationModeEnabled)
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
            var tableHolder = resolver.ResolveAndSetIfAvailable(context.Extensions);
            // slight duplication between saga persister and here when it comes to conventional tables
            // assuming the table will be created by the saga persister
            var sagaTable = tableHolder == null ? client.GetTableReference(typeof(TSagaData).Name) : tableHolder.Table;

            if (!context.Extensions.TryGet<TableInformation>(out _))
            {
                context.Extensions.Set(new TableInformation(sagaTable.Name));
            }

            if (context.Headers.TryGetValue(Headers.SagaId, out var sagaId))
            {
                context.Extensions.Set(new TableEntityPartitionKey(sagaId));
                return;
            }

            if (correlationProperty == SagaCorrelationProperty.None)
            {
                throw new Exception("The Azure Table saga persister doesn't support custom saga finders.");
            }

            if (migrationModeEnabled)
            {
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