namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.IO;
    using Features;
    using Logging;
    using Microsoft.Extensions.DependencyInjection;
    using Sagas;
    using Migration;
    using Newtonsoft.Json;

    class SagaStorage : Feature
    {
        internal SagaStorage()
        {
            Defaults(s =>
            {
                s.SetDefault(WellKnownConfigurationKeys.SagaStorageAssumeSecondaryIndicesExist, AzureStorageSagaDefaults.AssumeSecondaryIndicesExist);
                s.SetDefault(WellKnownConfigurationKeys.SagaStorageAssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey, AzureStorageSagaDefaults.AssumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey);
                s.SetDefault(WellKnownConfigurationKeys.SagaStorageCompatibilityMode, AzureStorageSagaDefaults.CompatibilityModeEnabled);
                s.SetDefault(WellKnownConfigurationKeys.SagaStorageConventionalTablePrefix, AzureStorageSagaDefaults.ConventionalTablePrefix);
                s.SetDefault(WellKnownConfigurationKeys.SagaJsonSerializer, JsonSerializer.Create());
                s.SetDefault(WellKnownConfigurationKeys.SagaReaderCreator, (Func<TextReader, JsonReader>)(reader => new JsonTextReader(reader)));
                s.SetDefault(WellKnownConfigurationKeys.SagaWriterCreator, (Func<TextWriter, JsonWriter>)(writer => new JsonTextWriter(writer)));

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
            // backdoor for testing
            var conventionalTablePrefix = context.Settings.Get<string>(WellKnownConfigurationKeys.SagaStorageConventionalTablePrefix);

            if (compatibilityModeEnabled)
            {
                var addition = assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey ? ", assuming the secondary index uses RowKey = PartitionKey," : string.Empty;
                Logger.Info($"The version of {nameof(AzureTablePersistence)} uses the migration mode and will fallback to lookup correlated sagas based on the secondary index{addition} if necessary.");
            }

            if (assumeSecondaryIndicesExist == false)
            {
                Logger.Warn($"The version of {nameof(AzureTablePersistence)} used is not configured to optimize sagas creation and might fall back to full table scanning to retrieve correlated sagas. It is suggested to migrate saga instances. Consult the upgrade guides for recommendations.");
            }

            var secondaryIndices = new SecondaryIndex(assumeSecondaryIndicesExist, assumeSecondaryKeyUsesANonEmptyRowKeySetToThePartitionKey);

            context.Services.AddSingleton<IProvidePartitionKeyFromSagaId>(provider =>
                new ProvidePartitionKeyFromSagaId(provider.GetRequiredService<IProvideCloudTableClient>(),
                    provider.GetRequiredService<TableHolderResolver>(), secondaryIndices, compatibilityModeEnabled, conventionalTablePrefix));

            var installerSettings = context.Settings.Get<SynchronizedStorageInstallerSettings>();
            var jsonSerializer = context.Settings.Get<JsonSerializer>(WellKnownConfigurationKeys.SagaJsonSerializer);
            var readerCreator = context.Settings.Get<Func<TextReader, JsonReader>>(WellKnownConfigurationKeys.SagaReaderCreator);
            var writerCreator = context.Settings.Get<Func<TextWriter, JsonWriter>>(WellKnownConfigurationKeys.SagaWriterCreator);

            context.Services.AddSingleton<ISagaPersister>(provider => new AzureSagaPersister(provider.GetRequiredService<IProvideCloudTableClient>(),
                installerSettings.Disabled, compatibilityModeEnabled, secondaryIndices, conventionalTablePrefix, jsonSerializer, readerCreator, writerCreator));
        }

        static readonly ILog Logger = LogManager.GetLogger<SagaStorage>();
    }
}