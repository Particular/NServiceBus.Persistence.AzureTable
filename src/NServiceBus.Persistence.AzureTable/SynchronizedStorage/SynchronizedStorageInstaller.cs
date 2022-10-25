namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Features;
    using Installation;
    using Logging;
    using Microsoft.Extensions.DependencyInjection;
    using Settings;

    class SynchronizedStorageInstaller : INeedToInstallSomething
    {
        public SynchronizedStorageInstaller(IServiceProvider serviceProvider, IReadOnlySettings settings)
        {
            this.settings = settings;
            this.serviceProvider = serviceProvider;
        }

        public async Task Install(string identity, CancellationToken cancellationToken = default)
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
                await CreateTableIfNotExists(installerSettings, serviceProvider.GetRequiredService<IProvideTableServiceClient>(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
            {
                log.Error("Could not complete the installation. ", ex);
                throw;
            }
        }

        async Task CreateTableIfNotExists(SynchronizedStorageInstallerSettings installerSettings, IProvideTableServiceClient serviceClientProvider, CancellationToken cancellationToken)
        {
            var tableClient = serviceClientProvider.Client.GetTableClient(installerSettings.TableName);
            await tableClient.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
        }

        static ILog log = LogManager.GetLogger<SynchronizedStorageInstaller>();
        IServiceProvider serviceProvider;
        IReadOnlySettings settings;
    }
}