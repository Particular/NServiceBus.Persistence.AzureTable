namespace NServiceBus.Persistence.AzureTable
{
    using Features;

    class SubscriptionStorageInstallerFeature : Feature
    {
        public SubscriptionStorageInstallerFeature()
        {
            Defaults(s => s.SetDefault(new SubscriptionStorageInstallerSettings()));
            DependsOn<SubscriptionStorage>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var settings = context.Settings.Get<SubscriptionStorageInstallerSettings>();
            if (settings.Disabled)
            {
                return;
            }

            settings.TableName = context.Settings.GetSubscriptionTableName();
        }
    }
}