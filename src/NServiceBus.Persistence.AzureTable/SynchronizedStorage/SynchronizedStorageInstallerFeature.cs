namespace NServiceBus.Persistence.AzureTable
{
    using Features;

    class SynchronizedStorageInstallerFeature : Feature
    {
        public SynchronizedStorageInstallerFeature()
        {
            Defaults(s => s.SetDefault(new SynchronizedStorageInstallerSettings()));
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var settings = context.Settings.Get<SynchronizedStorageInstallerSettings>();

            if (settings.Disabled)
            {
                return;
            }

            if (context.Settings.TryGet<TableInformation>(out var tableInformation))
            {
                settings.TableName = tableInformation.TableName;
            }
        }
    }
}