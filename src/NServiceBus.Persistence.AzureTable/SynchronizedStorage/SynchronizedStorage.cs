namespace NServiceBus.Persistence.AzureTable
{
    using Features;

    class SynchronizedStorage : Feature
    {
        public SynchronizedStorage()
        {
            Defaults(s =>
            {
                s.EnableFeatureByDefault<SynchronizedStorageInstallerFeature>();
            });
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            // If a client has been registered in the container, it will added later in the configuration process and replace any client set here
            context.Settings.TryGet(out IProvideCloudTableClient cloudTableClientProvider);
            context.Container.ConfigureComponent(builder => cloudTableClientProvider ?? new ThrowIfNoCloudTableClientProvider(), DependencyLifecycle.SingleInstance);

            TableInformation? defaultTableInformation = null;
            if (context.Settings.TryGet<TableInformation>(out var info))
            {
                defaultTableInformation = info;
            }

            context.Settings.AddStartupDiagnosticsSection(
                "NServiceBus.Persistence.AzureTable.StorageSession",
                new
                {
                    ConnectionMechanism = cloudTableClientProvider is CloudTableClientFromConnectionString ? "ConnectionString" : "CloudTableClient",
                    DefaultTable = defaultTableInformation.HasValue ? defaultTableInformation.Value.TableName : "Not used",
                });

            context.Container.ConfigureComponent(b => b.Build<CompletableSynchronizedStorageSessionAdapter>().AdaptedSession.AzureTablePersistenceSession(), DependencyLifecycle.InstancePerUnitOfWork);
            context.Container.ConfigureComponent(builder => new TableHolderResolver(builder.Build<IProvideCloudTableClient>(), defaultTableInformation), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<ISynchronizedStorage>(builder => new StorageSessionFactory(builder.Build<TableHolderResolver>()), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<ISynchronizedStorageAdapter>(provider => new StorageSessionAdapter(), DependencyLifecycle.SingleInstance);
        }
    }
}