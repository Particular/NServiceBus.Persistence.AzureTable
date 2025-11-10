namespace NServiceBus.Persistence.AzureTable
{
    using Features;
    using Microsoft.Extensions.DependencyInjection;

    class SynchronizedStorage : Feature
    {
        public SynchronizedStorage()
        {
            DependsOn<Features.SynchronizedStorage>();

            Defaults(s => s.SetDefault(new SynchronizedStorageInstallerSettings()));
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            // If a client has been registered in the container, it will added later in the configuration process and replace any client set here
            context.Settings.TryGet(out IProvideTableServiceClient tableServiceClientProvider);
            context.Services.AddSingleton(tableServiceClientProvider ?? new ThrowIfNoTableServiceClientProvider());

            var settings = context.Settings.Get<SynchronizedStorageInstallerSettings>();

            TableInformation? defaultTableInformation = null;
            if (context.Settings.TryGet<TableInformation>(out var info))
            {
                defaultTableInformation = info;
                settings.TableName = info.TableName;
            }

            if (!settings.Disabled && !string.IsNullOrEmpty(settings.TableName))
            {
                context.AddInstaller<SynchronizedStorageInstaller>();
            }

            context.Settings.AddStartupDiagnosticsSection(
                "NServiceBus.Persistence.AzureTable.StorageSession",
                new
                {
                    ConnectionMechanism = tableServiceClientProvider is TableServiceClientFromConnectionString ? "ConnectionString" : "TableServiceClient",
                    DefaultTable = defaultTableInformation.HasValue ? defaultTableInformation.Value.TableName : "Not used",
                });

            context.Services.AddSingleton(provider => new TableClientHolderResolver(provider.GetRequiredService<IProvideTableServiceClient>(), defaultTableInformation));

            context.Services.AddScoped<ICompletableSynchronizedStorageSession>(provider =>
                new AzureStorageSynchronizedStorageSession(provider.GetRequiredService<TableClientHolderResolver>()));
            context.Services.AddScoped(sp => (sp.GetService<ICompletableSynchronizedStorageSession>() as IAzureTableStorageSession)!);

            context.Settings.TryGet(out SynchronizedStorageInstallerSettings installerSettings);
            context.Services.AddSingleton(new TableCreator(installerSettings != null && installerSettings.Disabled));
        }
    }
}