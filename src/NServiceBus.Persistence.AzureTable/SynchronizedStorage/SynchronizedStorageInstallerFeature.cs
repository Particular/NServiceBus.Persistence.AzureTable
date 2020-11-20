namespace NServiceBus.Persistence.AzureTable
{
    using Features;

    class SynchronizedStorageInstallerFeature : Feature
    {
        public SynchronizedStorageInstallerFeature()
        {
            Defaults(s => s.SetDefault<SynchronizedStorageInstallerSettings>(new SynchronizedStorageInstallerSettings()));
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var settings = context.Settings.Get<SynchronizedStorageInstallerSettings>();

            // if it hasn't been explicitly disabled installer settings need to be considered
            if (!settings.Disabled)
            {
                settings.Disabled = !context.Settings.GetOrDefault<bool>("Installers.Enable");
            }

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