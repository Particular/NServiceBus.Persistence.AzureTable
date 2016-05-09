namespace NServiceBus
{
    using System.Configuration;
    using Features;
    using Persistence.AzureStorage;

    public class AzureStorageSagaPersistence : Feature
    {
        internal AzureStorageSagaPersistence()
        {
            DependsOn<Features.Sagas>();
            Defaults(s =>
            {
                var defaultConnectionString = ConfigurationManager.AppSettings["NServiceBus/Persistence"];
                s.SetDefault("AzureSagaStorage.ConnectionString", defaultConnectionString);
                s.SetDefault("AzureSagaStorage.CreateSchema", AzureStorageSagaDefaults.CreateSchema);
            });
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var connectionstring = context.Settings.Get<string>("AzureSagaStorage.ConnectionString");
            var updateSchema = context.Settings.Get<bool>("AzureSagaStorage.CreateSchema");

            context.Container.ConfigureComponent(builder => new AzureSagaPersister(connectionstring, updateSchema), DependencyLifecycle.InstancePerCall);
        }
    }
}