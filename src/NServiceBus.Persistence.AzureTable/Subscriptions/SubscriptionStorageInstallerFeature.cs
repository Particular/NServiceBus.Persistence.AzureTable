namespace NServiceBus.Persistence.AzureTable
{
    using Features;

    class SubscriptionStorageInstallerFeature : Feature
    {
        public SubscriptionStorageInstallerFeature()
        {
            Defaults(s => s.SetDefault<SubscriptionStorageInstallerSettings>(new SubscriptionStorageInstallerSettings()));
            DependsOn<SubscriptionStorage>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var settings = context.Settings.Get<SubscriptionStorageInstallerSettings>();

            // if it hasn't been explicitly disabled installer settings need to be considered
            if (!settings.Disabled)
            {
                settings.Disabled = !context.Settings.GetOrDefault<bool>("Installers.Enable");
            }

            if (settings.Disabled)
            {
                return;
            }

            settings.TableName = context.Settings.GetSubscriptionTableName();
        }
    }
}