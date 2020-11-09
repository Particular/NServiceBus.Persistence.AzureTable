namespace NServiceBus.Persistence.AzureTable
{
    using Features;
    using Logging;
    using Sagas;
    using Migration;

    class SagaStorage : Feature
    {
        internal SagaStorage()
        {
            Defaults(s =>
            {
                s.SetDefault(WellKnownConfigurationKeys.SagaStorageCreateSchema, AzureStorageSagaDefaults.CreateSchema);
                s.SetDefault(WellKnownConfigurationKeys.SagaStorageAssumeSecondaryIndicesExist, AzureStorageSagaDefaults.AssumeSecondaryIndicesExist);
                s.SetDefault(WellKnownConfigurationKeys.SagaStorageAssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey, AzureStorageSagaDefaults.AssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey);
                s.SetDefault(WellKnownConfigurationKeys.SagaStorageMigrationMode, AzureStorageSagaDefaults.MigrationModeEnabled);

                s.EnableFeatureByDefault<SynchronizedStorage>();
                s.SetDefault<ISagaIdGenerator>(new SagaIdGenerator());
            });
            DependsOn<SynchronizedStorage>();
            DependsOn<Features.Sagas>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var updateSchema = context.Settings.Get<bool>(WellKnownConfigurationKeys.SagaStorageCreateSchema);
            var migrationModeEnabled = context.Settings.Get<bool>(WellKnownConfigurationKeys.SagaStorageMigrationMode);
            var assumeSecondaryIndicesExist = context.Settings.Get<bool>(WellKnownConfigurationKeys.SagaStorageAssumeSecondaryIndicesExist);
            var assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey = context.Settings.Get<bool>(WellKnownConfigurationKeys.SagaStorageAssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey);

            if (migrationModeEnabled)
            {
                var addition = assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey? ", assuming the secondary index uses RowKey = PartitionKey," : string.Empty;
                Logger.Info($"The version of {nameof(AzureTablePersistence)} uses the migration mode and will fallback to lookup correlated sages based on the secondary index{addition} if necessary.");
            }

            if (assumeSecondaryIndicesExist == false)
            {
                Logger.Warn($"The version of {nameof(AzureTablePersistence)} used is not configured to optimize sagas creation and might fall back to full table scanning to retrieve correlated sagas. It is suggested to migrate saga instances. Consult the upgrade guides for recommendations.");
            }

            var secondaryIndices = new SecondaryIndex(assumeSecondaryIndicesExist, assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey);

            context.Container.ConfigureComponent<IProvidePartitionKeyFromSagaId>(builder =>
                new ProvidePartitionKeyFromSagaId(builder.Build<IProvideCloudTableClient>(),
                    builder.Build<TableHolderResolver>(), secondaryIndices, migrationModeEnabled), DependencyLifecycle.SingleInstance);

            context.Container.ConfigureComponent<ISagaPersister>(builder => new AzureSagaPersister(builder.Build<IProvideCloudTableClient>(), updateSchema, migrationModeEnabled, secondaryIndices), DependencyLifecycle.SingleInstance);
        }

        static readonly ILog Logger = LogManager.GetLogger<SagaStorage>();
    }
}