namespace NServiceBus
{
    using System;
    using Config;
    using Features;

    public class DetectObsoleteConfigurationSettings : Feature
    {
        public DetectObsoleteConfigurationSettings()
        {
            EnableByDefault();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var sagaPersisterConfig = context.Settings.GetConfigSection<AzureSagaPersisterConfig>();
            var subscriptionStorageConfig = context.Settings.GetConfigSection<AzureSubscriptionStorageConfig>();
            var timeoutPersisterConfig = context.Settings.GetConfigSection<AzureTimeoutPersisterConfig>();

            if (sagaPersisterConfig != null || subscriptionStorageConfig != null || timeoutPersisterConfig != null)
            {
                throw new NotSupportedException($"Configuration sections are no longer supported for Azure Storage Persistence. Switch to the code API by using `{nameof(PersistenceExtentions)}` instead.");
            }
        }
    }
}