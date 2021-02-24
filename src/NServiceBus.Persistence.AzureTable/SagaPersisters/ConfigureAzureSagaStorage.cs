namespace NServiceBus
{
    using System;
    using System.IO;
    using Configuration.AdvancedExtensibility;
    using Microsoft.Azure.Cosmos.Table;
    using Newtonsoft.Json;
    using Persistence.AzureTable;

    /// <summary>
    /// Configuration extensions for the sagas storage
    /// </summary>
    public static class ConfigureAzureSagaStorage
    {
        /// <summary>
        /// Connection string to use for sagas storage.
        /// </summary>
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> ConnectionString(this PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> config, string connectionString)
        {
            AzureStorageSagaGuard.CheckConnectionString(connectionString);

            config.GetSettings().Set<IProvideCloudTableClient>(new CloudTableClientFromConnectionString(connectionString));
            return config;
        }

        /// <summary>
        /// Cloud Table Client to use for the saga storage.
        /// </summary>
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> UseCloudTableClient(this PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> config, CloudTableClient client)
        {
            Guard.AgainstNull(nameof(client), client);

            var settings = config.GetSettings();
            settings.Set<IProvideCloudTableClient>(new CloudTableClientFromConfiguration(client));

            return config;
        }

        /// <summary>
        /// Disables the table creation for the Saga storage.
        /// </summary>
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> DisableTableCreation(this PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> config)
        {
            Guard.AgainstNull(nameof(config), config);

            var settings = config.GetSettings();
            settings.GetOrCreate<SynchronizedStorageInstallerSettings>().Disabled = true;

            return config;
        }

        /// <summary>
        /// Overrides the default settings used by the saga property serializer used for complex property types serialization that is not supported by default with tables.
        /// </summary>
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> JsonSettings(this PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> config, JsonSerializerSettings jsonSerializerSettings)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNull(nameof(jsonSerializerSettings), jsonSerializerSettings);

            var settings = config.GetSettings();
            settings.Set(WellKnownConfigurationKeys.SagaJsonSerializer, JsonSerializer.Create(jsonSerializerSettings));

            return config;
        }

        /// <summary>
        /// Overrides the reader creator to customize data deserialization.
        /// </summary>
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> ReaderCreator(this PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> config, Func<TextReader, JsonReader> readerCreator)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNull(nameof(readerCreator), readerCreator);

            var settings = config.GetSettings();

            settings.Set(WellKnownConfigurationKeys.SagaReaderCreator, readerCreator);

            return config;
        }

        /// <summary>
        /// Overrides the writer creator to customize data serialization.
        /// </summary>
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> WriterCreator(this PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> config, Func<StringWriter, JsonWriter> writerCreator)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNull(nameof(writerCreator), writerCreator);

            var settings = config.GetSettings();

            settings.Set(WellKnownConfigurationKeys.SagaWriterCreator, writerCreator);

            return config;
        }

        /// <summary>
        /// Configures the backward compatibility specific settings.
        /// </summary>
        public static CompatibilitySettings Compatibility(this PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> config)
        {
            Guard.AgainstNull(nameof(config), config);

            return new CompatibilitySettings(config.GetSettings());
        }
    }
}
