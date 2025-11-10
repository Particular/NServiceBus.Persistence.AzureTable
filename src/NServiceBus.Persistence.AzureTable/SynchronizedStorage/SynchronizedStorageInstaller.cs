namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Installation;
    using Logging;
    using Microsoft.Extensions.DependencyInjection;
    using Settings;

    class SynchronizedStorageInstaller(IServiceProvider serviceProvider, IReadOnlySettings settings)
        : INeedToInstallSomething
    {
        public async Task Install(string identity, CancellationToken cancellationToken = default)
        {
            var installerSettings = settings.Get<SynchronizedStorageInstallerSettings>();

            try
            {
                Log.Info("Creating default Saga and/or Outbox Table");
                await CreateTableIfNotExists(installerSettings, serviceProvider.GetRequiredService<IProvideTableServiceClient>(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
            {
                Log.Error("Could not complete the installation. ", ex);
                throw;
            }
        }

        static async Task CreateTableIfNotExists(SynchronizedStorageInstallerSettings installerSettings, IProvideTableServiceClient serviceClientProvider, CancellationToken cancellationToken)
        {
            var tableClient = serviceClientProvider.Client.GetTableClient(installerSettings.TableName);
            await tableClient.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
        }

        static readonly ILog Log = LogManager.GetLogger<SynchronizedStorageInstaller>();
    }
}