namespace NServiceBus
{
    using Features;
    using Logging;
    using Microsoft.Extensions.DependencyInjection;
    using Persistence.AzureStorage;
    using Persistence.AzureStorage.Config;
    using Sagas;

    /// <summary></summary>
    public class AzureStorageSagaPersistence : Feature
    {
        internal AzureStorageSagaPersistence()
        {
            Defaults(s =>
            {
#if NETFRAMEWORK
                var defaultConnectionString = System.Configuration.ConfigurationManager.AppSettings["NServiceBus/Persistence"];
                if (string.IsNullOrEmpty(defaultConnectionString) != true)
                {
                    logger.Warn(@"Connection string should be assigned using code API: var persistence = endpointConfiguration.UsePersistence<AzureStoragePersistence, StorageType.Sagas>();\npersistence.ConnectionString(""connectionString"");");
                }
#endif
                s.SetDefault(WellKnownConfigurationKeys.SagaStorageCreateSchema, AzureStorageSagaDefaults.CreateSchema);
                s.SetDefault(WellKnownConfigurationKeys.SagaStorageAssumeSecondaryIndicesExist, AzureStorageSagaDefaults.AssumeSecondaryIndicesExist);
                s.SetDefault(WellKnownConfigurationKeys.MigrationMode, AzureStorageSagaDefaults.MigrationModeEnabled);

                s.EnableFeatureByDefault<SynchronizedStorage>();
                s.SetDefault<ISagaIdGenerator>(new SagaIdGenerator());
            });
            DependsOn<SynchronizedStorage>();
            DependsOn<Features.Sagas>();
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var updateSchema = context.Settings.Get<bool>(WellKnownConfigurationKeys.SagaStorageCreateSchema);
            var migrationModeEnabled = context.Settings.Get<bool>(WellKnownConfigurationKeys.MigrationMode);
            var assumeSecondaryIndicesExist = context.Settings.Get<bool>(WellKnownConfigurationKeys.SagaStorageAssumeSecondaryIndicesExist);

            if (assumeSecondaryIndicesExist == false)
            {
                logger.Warn($"The version of {nameof(AzureStoragePersistence)} used is not configured to optimize sagas creation and might fall back to full table scanning to retrieve correlated sagas. It is suggested to migrate saga instances. Consult the upgrade guides for recommendations.");
            }

            context.Services.AddSingleton<ISagaPersister>(provider => new AzureSagaPersister(provider.GetRequiredService<IProvideCloudTableClient>(), updateSchema, migrationModeEnabled,assumeSecondaryIndicesExist));
        }

        static ILog logger = LogManager.GetLogger<AzureStorageSagaPersistence>();
    }
}