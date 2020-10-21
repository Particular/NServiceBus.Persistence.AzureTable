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
            var connectionstring = context.Settings.Get<string>(WellKnownConfigurationKeys.SagaStorageConnectionString);
            var updateSchema = context.Settings.Get<bool>(WellKnownConfigurationKeys.SagaStorageCreateSchema);
            var assumeSecondaryIndicesExist = context.Settings.Get<bool>(WellKnownConfigurationKeys.SagaStorageAssumeSecondaryIndicesExist);

            // TODO: Adjust when flag is flipped
            if (assumeSecondaryIndicesExist == false)
            {
                logger.Warn($"The version of {nameof(AzureStoragePersistence)} used is not configured to optimize sagas creation. To enable optimization, use '.{nameof(ConfigureAzureSagaStorage.AssumeSecondaryIndicesExist)}()' configuration API.");
            }

            context.Services.AddSingleton<ISagaPersister>(_ => new AzureSagaPersister(connectionstring, updateSchema, assumeSecondaryIndicesExist));
        }

        static ILog logger = LogManager.GetLogger<AzureStorageSagaPersistence>();
    }
}