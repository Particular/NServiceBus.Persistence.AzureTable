namespace NServiceBus
{
    using System;
    using Features;
    using Logging;
    using Persistence;
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
            });
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var connectionstring = context.Settings.Get<string>(WellKnownConfigurationKeys.SagaStorageConnectionString);
            var updateSchema = context.Settings.Get<bool>(WellKnownConfigurationKeys.SagaStorageCreateSchema);

            if (!context.Settings.TryGet<bool>(WellKnownConfigurationKeys.SagaStorageAssumeSecondaryIndicesExist, out var assumeSecondaryIndicesExist))
            {
                throw new Exception($"To optimize sagas creation, saga persistence has to be configured using {nameof(PersistenceExtensions<AzureStoragePersistence, StorageType.Sagas>)}.{nameof(ConfigureAzureSagaStorage.AssumeSecondaryIndicesExist)} API.");
            }

            context.Container.ConfigureComponent(builder => new AzureSagaPersister(connectionstring, updateSchema, assumeSecondaryIndicesExist), DependencyLifecycle.InstancePerCall);
        }

        static ILog logger = LogManager.GetLogger<AzureStorageSagaPersistence>();
    }
}