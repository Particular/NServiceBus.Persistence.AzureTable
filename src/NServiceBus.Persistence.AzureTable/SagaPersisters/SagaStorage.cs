namespace NServiceBus.Persistence.AzureTable
{
    using Features;
    using Logging;
    using Microsoft.Extensions.DependencyInjection;
    using Sagas;
    using Migration;

    class SagaStorage : Feature
    {
        internal SagaStorage()
        {
            Defaults(s =>
            {
                s.SetDefault(WellKnownConfigurationKeys.SagaStorageAssumeSecondaryIndicesExist, AzureStorageSagaDefaults.AssumeSecondaryIndicesExist);
                s.SetDefault(WellKnownConfigurationKeys.SagaStorageAssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey, AzureStorageSagaDefaults.AssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey);
                s.SetDefault(WellKnownConfigurationKeys.SagaStorageCompatibilityMode, AzureStorageSagaDefaults.CompatibilityModeEnabled);

                s.EnableFeatureByDefault<SynchronizedStorage>();
                s.SetDefault<ISagaIdGenerator>(new SagaIdGenerator());
            });
            DependsOn<SynchronizedStorage>();
            DependsOn<Features.Sagas>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var compatibilityModeEnabled = context.Settings.Get<bool>(WellKnownConfigurationKeys.SagaStorageCompatibilityMode);
            var assumeSecondaryIndicesExist = context.Settings.Get<bool>(WellKnownConfigurationKeys.SagaStorageAssumeSecondaryIndicesExist);
            var assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey = context.Settings.Get<bool>(WellKnownConfigurationKeys.SagaStorageAssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey);

            if (compatibilityModeEnabled)
            {
                var addition = assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey? ", assuming the secondary index uses RowKey = PartitionKey," : string.Empty;
                Logger.Info($"The version of {nameof(AzureTablePersistence)} uses the migration mode and will fallback to lookup correlated sagas based on the secondary index{addition} if necessary.");
            }

            if (assumeSecondaryIndicesExist == false)
            {
                Logger.Warn($"The version of {nameof(AzureTablePersistence)} used is not configured to optimize sagas creation and might fall back to full table scanning to retrieve correlated sagas. It is suggested to migrate saga instances. Consult the upgrade guides for recommendations.");
            }

            var secondaryIndices = new SecondaryIndex(assumeSecondaryIndicesExist, assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey);

            context.Services.AddSingleton<IProvidePartitionKeyFromSagaId>(provider =>
                new ProvidePartitionKeyFromSagaId(provider.GetRequiredService<IProvideCloudTableClient>(),
                    provider.GetRequiredService<TableHolderResolver>(), secondaryIndices, compatibilityModeEnabled));

            var installerSettings = context.Settings.Get<SynchronizedStorageInstallerSettings>();
            context.Services.AddSingleton<ISagaPersister>(provider => new AzureSagaPersister(provider.GetRequiredService<IProvideCloudTableClient>(),
                installerSettings.Disabled, compatibilityModeEnabled, secondaryIndices));
        }

        static readonly ILog Logger = LogManager.GetLogger<SagaStorage>();
    }
}