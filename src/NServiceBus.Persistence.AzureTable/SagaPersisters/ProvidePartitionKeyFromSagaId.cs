namespace NServiceBus.Persistence.AzureTable.Migration
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Sagas;
    using Microsoft.Azure.Cosmos.Table;

    class ProvidePartitionKeyFromSagaId : IProvidePartitionKeyFromSagaId
    {
        public ProvidePartitionKeyFromSagaId(IProvideCloudTableClient tableClientProvider, TableHolderResolver resolver, SecondaryIndex secondaryIndex, bool compatibilityMode, string conventionalTablePrefix)
        {
            this.conventionalTablePrefix = conventionalTablePrefix;
            this.compatibilityMode = compatibilityMode;
            this.resolver = resolver;
            this.secondaryIndex = secondaryIndex;
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
            var sagaTable = tableHolder == null ? client.GetTableReference($"{conventionalTablePrefix}{typeof(TSagaData).Name}") : tableHolder.Table;

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

            if (compatibilityMode)
            {
                var nullableSagaId = await secondaryIndex.FindSagaId<TSagaData>(sagaTable, correlationProperty)
                    .ConfigureAwait(false);

                if (nullableSagaId.HasValue)
                {
                    context.Extensions.Set(new TableEntityPartitionKey(nullableSagaId.Value.ToString()));
                    return;
                }
            }

            var deterministicSagaId = SagaIdGenerator.Generate<TSagaData>(correlationProperty);
            context.Extensions.Set(new TableEntityPartitionKey(deterministicSagaId.ToString()));
        }

        private readonly SecondaryIndex secondaryIndex;
        private CloudTableClient client;
        private readonly TableHolderResolver resolver;
        private readonly bool compatibilityMode;
        private readonly string conventionalTablePrefix;
    }
}