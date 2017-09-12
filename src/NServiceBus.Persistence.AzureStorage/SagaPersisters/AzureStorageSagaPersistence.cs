namespace NServiceBus
{
    using Features;
    using Logging;
    using Persistence.AzureStorage;
    using Persistence.AzureStorage.Config;

    /// <summary></summary>
    public class AzureStorageSagaPersistence : Feature
    {
        internal AzureStorageSagaPersistence()
        {
            DependsOn<Features.Sagas>();
            Defaults(s =>
            {
#if NET452
                var defaultConnectionString = System.Configuration.ConfigurationManager.AppSettings["NServiceBus/Persistence"];
                if (string.IsNullOrEmpty(defaultConnectionString) != true)
                {
                    logger.Warn(@"Connection string should be assigned using code API: var persistence = endpointConfiguration.UsePersistence<AzureStoragePersistence, StorageType.Timeouts>();\npersistence.ConnectionString(""connectionString"");");
                }
#endif
                s.SetDefault(WellKnownConfigurationKeys.SagaStorageCreateSchema, AzureStorageSagaDefaults.CreateSchema);
                s.SetDefault(WellKnownConfigurationKeys.SagaStorageAssumeSecondaryIndicesExist, AzureStorageSagaDefaults.AssumeSecondaryIndicesExist);
            });
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var connectionstring = context.Settings.Get<string>(WellKnownConfigurationKeys.SagaStorageConnectionString);
            var updateSchema = context.Settings.Get<bool>(WellKnownConfigurationKeys.SagaStorageCreateSchema);
            var assumeSecondaryIndicesExist = context.Settings.Get<bool>(WellKnownConfigurationKeys.SagaStorageAssumeSecondaryIndicesExist);

            if (assumeSecondaryIndicesExist == false)
            {
                logger.Warn($"The version of {nameof(AzureStoragePersistence)} used is not configured to optimize sagas creation. To enable optimization, use '.{nameof(ConfigureAzureSagaStorage.AssumeSecondaryIndicesExist)}()' configuration API.");
            }

            context.Container.ConfigureComponent(builder => new AzureSagaPersister(connectionstring, updateSchema, assumeSecondaryIndicesExist), DependencyLifecycle.InstancePerCall);
        }

        static ILog logger = LogManager.GetLogger<AzureStorageSagaPersistence>();
    }
}