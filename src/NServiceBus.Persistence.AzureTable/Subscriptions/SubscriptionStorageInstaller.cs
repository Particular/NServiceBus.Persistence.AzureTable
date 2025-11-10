namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Installation;
    using Logging;
    using Microsoft.Extensions.DependencyInjection;
    using Settings;

    class SubscriptionStorageInstaller(IServiceProvider serviceProvider, IReadOnlySettings settings)
        : INeedToInstallSomething
    {
        public async Task Install(string identity, CancellationToken cancellationToken = default)
        {
            var installerSettings = settings.Get<SubscriptionStorageInstallerSettings>();

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

        static async Task CreateTableIfNotExists(SubscriptionStorageInstallerSettings installerSettings, IProvideTableServiceClientForSubscriptions serviceClientProvider, CancellationToken cancellationToken)
        {
            var tableClient = serviceClientProvider.Client.GetTableClient(installerSettings.TableName);
            await tableClient.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
        }

        static readonly ILog Logger = LogManager.GetLogger<SynchronizedStorageInstaller>();
    }
}