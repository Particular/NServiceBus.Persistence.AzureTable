namespace NServiceBus.Persistence.AzureTable.Migration
{
    using System;
    using System.Threading.Tasks;
    using Azure.Data.Tables;
    using Pipeline;
    using Sagas;

    class ProvidePartitionKeyFromSagaId : IProvidePartitionKeyFromSagaId
    {
        public ProvidePartitionKeyFromSagaId(IProvideTableServiceClient tableServiceClientProvider, TableClientHolderResolver resolver, string conventionalTablePrefix)
        {
            this.conventionalTablePrefix = conventionalTablePrefix;
            this.resolver = resolver;
            tableServiceClient = tableServiceClientProvider.Client;
        }

        public Task SetPartitionKey<TSagaData>(IIncomingLogicalMessageContext context,
            SagaCorrelationProperty correlationProperty)
            where TSagaData : IContainSagaData
        {
            if (context.Extensions.TryGet<TableEntityPartitionKey>(out _))
            {
                return Task.CompletedTask;
            }
            var tableHolder = resolver.ResolveAndSetIfAvailable(context.Extensions);
            // slight duplication between saga persister and here when it comes to conventional tables
            // assuming the table will be created by the saga persister
            var sagaTable = tableHolder == null ? tableServiceClient.GetTableClient($"{conventionalTablePrefix}{typeof(TSagaData).Name}") : tableHolder.TableClient;

            if (!context.Extensions.TryGet<TableInformation>(out _))
            {
                context.Extensions.Set(new TableInformation(sagaTable.Name));
            }

            if (context.Headers.TryGetValue(Headers.SagaId, out var sagaId))
            {
                var tableEntityPartitionKey = new TableEntityPartitionKey(sagaId);
                context.Extensions.Set(tableEntityPartitionKey);
                return Task.CompletedTask;
            }

            if (correlationProperty == SagaCorrelationProperty.None)
            {
                throw new Exception("The Azure Table saga persister doesn't support custom saga finders.");
            }

            var deterministicSagaId = SagaIdGenerator.Generate<TSagaData>(correlationProperty);
            context.Extensions.Set(new TableEntityPartitionKey(deterministicSagaId.ToString()));

            return Task.CompletedTask;
        }

        readonly TableServiceClient tableServiceClient;
        readonly TableClientHolderResolver resolver;
        readonly string conventionalTablePrefix;
    }
}