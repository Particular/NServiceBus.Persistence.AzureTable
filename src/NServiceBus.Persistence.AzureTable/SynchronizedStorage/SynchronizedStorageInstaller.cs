namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.Threading.Tasks;
    using ObjectBuilder;
    using Installation;
    using Logging;
    using Features;
    using Settings;

    class SynchronizedStorageInstaller : INeedToInstallSomething
    {
        public SynchronizedStorageInstaller(IBuilder builder, ReadOnlySettings settings)
        {
            this.settings = settings;
            this.builder = builder;
        }

        public async Task Install(string identity)
        {
            if (!settings.IsFeatureActive(typeof(SynchronizedStorage)))
            {
                return;
            }

            var installerSettings = settings.Get<SynchronizedStorageInstallerSettings>();
            if (installerSettings.Disabled || string.IsNullOrEmpty(installerSettings.TableName))
            {
                return;
            }

            try
            {
                log.Info("Creating default Saga and/or Outbox Table");
                await CreateTableIfNotExists(installerSettings, builder.Build<IProvideCloudTableClient>()).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                log.Error("Could not complete the installation. ", e);
                throw;
            }
        }

        async Task CreateTableIfNotExists(SynchronizedStorageInstallerSettings installerSettings, IProvideCloudTableClient clientProvider)
        {
            var cloudTable = clientProvider.Client.GetTableReference(installerSettings.TableName);
            await cloudTable.CreateIfNotExistsAsync().ConfigureAwait(false);
        }

        static ILog log = LogManager.GetLogger<SynchronizedStorageInstaller>();
        private IBuilder builder;
        private ReadOnlySettings settings;
    }
}