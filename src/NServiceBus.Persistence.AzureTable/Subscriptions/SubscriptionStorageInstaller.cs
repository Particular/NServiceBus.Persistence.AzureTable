namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.Threading.Tasks;
    using ObjectBuilder;
    using Installation;
    using Logging;
    using Settings;
    using Features;

    class SubscriptionStorageInstaller : INeedToInstallSomething
    {
        public SubscriptionStorageInstaller(IBuilder builder, ReadOnlySettings settings)
        {
            this.settings = settings;
            this.builder = builder;
        }

        public async Task Install(string identity)
        {
            if (!settings.IsFeatureActive(typeof(SubscriptionStorage)))
            {
                return;
            }

            var installerSettings = settings.Get<SubscriptionStorageInstallerSettings>();
            if (installerSettings.Disabled)
            {
                return;
            }

            try
            {
                Logger.Info("Creating Subscription Table");
                await CreateTableIfNotExists(installerSettings, builder.Build<IProvideCloudTableClientForSubscriptions>()).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error("Could not complete the installation. ", e);
                throw;
            }
        }

        async Task CreateTableIfNotExists(SubscriptionStorageInstallerSettings installerSettings, IProvideCloudTableClientForSubscriptions clientProvider)
        {
            var cloudTable = clientProvider.Client.GetTableReference(installerSettings.TableName);
            await cloudTable.CreateIfNotExistsAsync().ConfigureAwait(false);
        }

        private IBuilder builder;
        static readonly ILog Logger = LogManager.GetLogger<SynchronizedStorageInstaller>();
        private ReadOnlySettings settings;
    }
}