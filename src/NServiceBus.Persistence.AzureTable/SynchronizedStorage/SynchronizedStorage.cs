namespace NServiceBus.Persistence.AzureTable
{
    using Features;
    using Microsoft.Extensions.DependencyInjection;

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
            context.Services.AddSingleton(cloudTableClientProvider ?? new ThrowIfNoCloudTableClientProvider());

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

            context.Services.AddSingleton(provider => new TableHolderResolver(provider.GetRequiredService<IProvideCloudTableClient>(), defaultTableInformation));

            context.Services.AddScoped<ICompletableSynchronizedStorageSession, SynchronizedStorageSession>(provider =>
                new SynchronizedStorageSession(provider.GetRequiredService<TableHolderResolver>()));
            context.Services.AddScoped(provider => provider.GetRequiredService<ICompletableSynchronizedStorageSession>().AzureTablePersistenceSession());

        }
    }
}