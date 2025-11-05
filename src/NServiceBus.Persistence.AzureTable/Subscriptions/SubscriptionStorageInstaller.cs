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

    class SubscriptionStorageInstaller : INeedToInstallSomething
    {
        public SubscriptionStorageInstaller(IServiceProvider serviceProvider, IReadOnlySettings settings)
        {
            this.settings = settings;
            this.serviceProvider = serviceProvider;
        }

        public async Task Install(string identity, CancellationToken cancellationToken = default)
        {
            if (!settings.IsFeatureActive<SubscriptionStorage>())
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
                await CreateTableIfNotExists(installerSettings, serviceProvider.GetRequiredService<IProvideTableServiceClientForSubscriptions>(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
            {
                Logger.Error("Could not complete the installation. ", ex);
                throw;
            }
        }

        async Task CreateTableIfNotExists(SubscriptionStorageInstallerSettings installerSettings, IProvideTableServiceClientForSubscriptions serviceClientProvider, CancellationToken cancellationToken)
        {
            var tableClient = serviceClientProvider.Client.GetTableClient(installerSettings.TableName);
            await tableClient.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
        }

        IServiceProvider serviceProvider;

        static readonly ILog Logger = LogManager.GetLogger<SynchronizedStorageInstaller>();
        IReadOnlySettings settings;
    }
}