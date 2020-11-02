namespace NServiceBus.Persistence.AzureStorage
{
    using Features;
    using Microsoft.Extensions.DependencyInjection;

    class SynchronizedStorage : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            // If a client has been registered in the container, it will added later in the configuration process and replace any client set here
            context.Settings.TryGet(out IProvideCloudTableClient cloudTableClientProvider);
            context.Services.AddSingleton(cloudTableClientProvider ?? new ThrowIfNoCloudTableClientProvider());

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