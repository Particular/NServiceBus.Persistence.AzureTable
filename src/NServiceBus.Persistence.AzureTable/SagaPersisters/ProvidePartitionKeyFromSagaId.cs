namespace NServiceBus.Persistence.AzureTable.Migration
{
    using System;
    using System.Threading.Tasks;
    using Azure.Data.Tables;
    using Pipeline;
    using Sagas;

    class ProvidePartitionKeyFromSagaId : IProvidePartitionKeyFromSagaId
    {
        public ProvidePartitionKeyFromSagaId(IProvideTableServiceClient tableServiceClientProvider, TableClientHolderResolver resolver, SecondaryIndex secondaryIndex, bool compatibilityMode, string conventionalTablePrefix)
        {
            this.conventionalTablePrefix = conventionalTablePrefix;
            this.compatibilityMode = compatibilityMode;
            this.resolver = resolver;
            this.secondaryIndex = secondaryIndex;
            client = tableServiceClientProvider.Client;
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
            var sagaTable = tableHolder == null ? client.GetTableClient($"{conventionalTablePrefix}{typeof(TSagaData).Name}") : tableHolder.TableClient;

            if (!context.Extensions.TryGet<TableInformation>(out _))
            {
                context.Extensions.Set(new TableInformation(sagaTable.Name));
            }

            if (context.Headers.TryGetValue(Headers.SagaId, out var sagaId))
            {
                var tableEntityPartitionKey = new TableEntityPartitionKey(sagaId);
                context.Extensions.Set(tableEntityPartitionKey);
                return;
            }

            if (correlationProperty == SagaCorrelationProperty.None)
            {
                throw new Exception("The Azure Table saga persister doesn't support custom saga finders.");
            }

            if (compatibilityMode)
            {
                var nullableSagaId = await secondaryIndex.FindSagaId<TSagaData>(sagaTable, correlationProperty, context.CancellationToken)
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

        readonly SecondaryIndex secondaryIndex;
        TableServiceClient client;
        readonly TableClientHolderResolver resolver;
        readonly bool compatibilityMode;
        readonly string conventionalTablePrefix;
    }
}