namespace NServiceBus
{
    using System.Configuration;
    using Features;
    using Logging;
    using Persistence.AzureStorage;
    using Persistence.AzureStorage.Config;

    public class AzureStorageSagaPersistence : Feature
    {
        internal AzureStorageSagaPersistence()
        {
            DependsOn<Features.Sagas>();
            Defaults(s =>
            {
                var defaultConnectionString = ConfigurationManager.AppSettings["NServiceBus/Persistence"];
                s.SetDefault(WellKnownConfigurationKeys.SagaStorageConnectionString, defaultConnectionString);
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
                log.Warn($"The version of {nameof(AzureStoragePersistence)} used is not configured to optimize sagas creation. To enable optimization, use '.{nameof(ConfigureAzureSagaStorage.AssumeSecondaryIndicesExist)}()' configuration API.");
            }

            context.Container.ConfigureComponent(builder => new AzureSagaPersister(connectionstring, updateSchema, assumeSecondaryIndicesExist), DependencyLifecycle.InstancePerCall);
        }

        static ILog log = LogManager.GetLogger<AzureStorageSagaPersistence>();
    }
}