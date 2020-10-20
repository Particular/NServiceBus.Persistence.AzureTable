namespace NServiceBus.Persistence.AzureStorage
{
    using Features;
    using Microsoft.Extensions.DependencyInjection;

    class SynchronizedStorage : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            // TODO check registration
            // if (!context.Services.HasComponent<IProvideCloudTableClient>())
            // {
            //     context.Container.ConfigureComponent(context.Settings.Get<IProvideCloudTableClient>, DependencyLifecycle.SingleInstance);
            // }
            TableInformation? defaultTableInformation = null;
            if (context.Settings.TryGet<TableInformation>(out var info))
            {
                defaultTableInformation = info;
            }

            var currentSharedTransactionalBatchHolder = new CurrentSharedTransactionalBatchHolder();

            // TODO, fix injection
            // context.Container.ConfigureComponent(_ => currentSharedTransactionalBatchHolder.Current, DependencyLifecycle.InstancePerCall);
            context.Services.AddSingleton(provider => new TableHolderResolver(provider.GetRequiredService<IProvideCloudTableClient>(), defaultTableInformation));
            context.Services.AddSingleton(provider => new StorageSessionFactory(provider.GetRequiredService<TableHolderResolver>(), currentSharedTransactionalBatchHolder));
            context.Services.AddSingleton(provider => new StorageSessionAdapter(currentSharedTransactionalBatchHolder));
            context.Pipeline.Register(new CurrentSharedTransactionalBatchBehavior(currentSharedTransactionalBatchHolder), "Manages the lifecycle of the current storage session.");
        }
    }
}