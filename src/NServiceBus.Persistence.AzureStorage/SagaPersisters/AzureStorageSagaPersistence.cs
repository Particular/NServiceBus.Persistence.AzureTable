namespace NServiceBus
{
    using System.Configuration;
    using Features;
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
            });
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var connectionstring = context.Settings.Get<string>(WellKnownConfigurationKeys.SagaStorageConnectionString);
            var updateSchema = context.Settings.Get<bool>(WellKnownConfigurationKeys.SagaStorageCreateSchema);

            context.Container.ConfigureComponent(builder => new AzureSagaPersister(connectionstring, updateSchema), DependencyLifecycle.InstancePerCall);
        }
    }
}