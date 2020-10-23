namespace NServiceBus.Persistence.AzureStorage
{
    using System.Linq;
    using Features;
    using Microsoft.Extensions.DependencyInjection;

    class SynchronizedStorage : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            if (!context.Services.Any(x => x.ServiceType == typeof(IProvideCloudTableClient)))
            {
                context.Services.AddSingleton(context.Settings.Get<IProvideCloudTableClient>());
            }

            TableInformation? defaultTableInformation = null;
            if (context.Settings.TryGet<TableInformation>(out var info))
            {
                defaultTableInformation = info;
            }

            var currentSharedTransactionalBatchHolder = new CurrentSharedTransactionalBatchHolder();

            context.Services.AddTransient<IAzureStorageStorageSession>(_ => currentSharedTransactionalBatchHolder.Current);
            context.Services.AddSingleton(provider => new TableHolderResolver(provider.GetRequiredService<IProvideCloudTableClient>(), defaultTableInformation));
            context.Services.AddSingleton<ISynchronizedStorage>(provider => new StorageSessionFactory(provider.GetRequiredService<TableHolderResolver>(), currentSharedTransactionalBatchHolder));
            context.Services.AddSingleton<ISynchronizedStorageAdapter>(provider => new StorageSessionAdapter(currentSharedTransactionalBatchHolder));
            context.Pipeline.Register(new CurrentSharedTransactionalBatchBehavior(currentSharedTransactionalBatchHolder), "Manages the lifecycle of the current storage session.");
        }
    }
}