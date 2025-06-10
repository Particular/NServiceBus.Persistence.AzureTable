namespace NServiceBus
{
    using System;
    using System.IO;
    using Azure.Data.Tables;
    using Configuration.AdvancedExtensibility;
    using Newtonsoft.Json;
    using Persistence.AzureTable;
    using JsonSerializer = Newtonsoft.Json.JsonSerializer;

    /// <summary>
    /// Configuration extensions for the sagas storage
    /// </summary>
    public static partial class ConfigureAzureSagaStorage
    {
        /// <summary>
        /// Connection string to use for sagas storage.
        /// </summary>
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> ConnectionString(this PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> config, string connectionString)
        {
            AzureStorageSagaGuard.CheckConnectionString(connectionString);

            config.GetSettings().Set<IProvideTableServiceClient>(new TableServiceClientFromConnectionString(connectionString));
            return config;
        }

        /// <summary>
        /// TableServiceClient to use for the saga storage.
        /// </summary>
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> UseTableServiceClient(this PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> config, TableServiceClient client)
        {
            ArgumentNullException.ThrowIfNull(client);

            var settings = config.GetSettings();
            settings.Set<IProvideTableServiceClient>(new TableServiceClientFromConfiguration(client));

            return config;
        }

        /// <summary>
        /// Disables the table creation for the Saga storage.
        /// </summary>
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> DisableTableCreation(this PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> config)
        {
            ArgumentNullException.ThrowIfNull(config);

            var settings = config.GetSettings();
            settings.GetOrCreate<SynchronizedStorageInstallerSettings>().Disabled = true;

            return config;
        }

        /// <summary>
        /// Overrides the default settings used by the saga property serializer used for complex property types serialization that is not supported by default with tables.
        /// </summary>
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> JsonSettings(this PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> config, JsonSerializerSettings jsonSerializerSettings)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(jsonSerializerSettings);

            var settings = config.GetSettings();
            settings.Set(WellKnownConfigurationKeys.SagaJsonSerializer, JsonSerializer.Create(jsonSerializerSettings));

            return config;
        }

        /// <summary>
        /// Overrides the reader creator to customize data deserialization.
        /// </summary>
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> ReaderCreator(this PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> config, Func<TextReader, JsonReader> readerCreator)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(readerCreator);

            var settings = config.GetSettings();

            settings.Set(WellKnownConfigurationKeys.SagaReaderCreator, readerCreator);

            return config;
        }

        /// <summary>
        /// Overrides the writer creator to customize data serialization.
        /// </summary>
        public static PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> WriterCreator(this PersistenceExtensions<AzureTablePersistence, StorageType.Sagas> config, Func<StringWriter, JsonWriter> writerCreator)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(writerCreator);

            var settings = config.GetSettings();

            settings.Set(WellKnownConfigurationKeys.SagaWriterCreator, writerCreator);

            return config;
        }
    }
}
