namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.IO;
    using Features;
    using Microsoft.Extensions.DependencyInjection;
    using Migration;
    using Newtonsoft.Json;
    using Sagas;

    class SagaStorage : Feature
    {
        internal SagaStorage()
        {
            Defaults(s =>
            {
                s.SetDefault(WellKnownConfigurationKeys.SagaStorageConventionalTablePrefix, AzureStorageSagaDefaults.ConventionalTablePrefix);
                s.SetDefault(WellKnownConfigurationKeys.SagaJsonSerializer, JsonSerializer.Create());
                s.SetDefault(WellKnownConfigurationKeys.SagaReaderCreator, (Func<TextReader, JsonReader>)(reader => new JsonTextReader(reader)));
                s.SetDefault(WellKnownConfigurationKeys.SagaWriterCreator, (Func<TextWriter, JsonWriter>)(writer => new JsonTextWriter(writer)));

                s.EnableFeatureByDefault<SynchronizedStorage>();
                s.SetDefault<ISagaIdGenerator>(new SagaIdGenerator());
            });

            DependsOn<Sagas>();
            DependsOn<SynchronizedStorage>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            // backdoor for testing
            var conventionalTablePrefix = context.Settings.Get<string>(WellKnownConfigurationKeys.SagaStorageConventionalTablePrefix);

            context.Services.AddSingleton<IProvidePartitionKeyFromSagaId>(provider =>
                new ProvidePartitionKeyFromSagaId(provider.GetRequiredService<IProvideTableServiceClient>(), provider.GetRequiredService<TableClientHolderResolver>(), conventionalTablePrefix));

            var installerSettings = context.Settings.Get<SynchronizedStorageInstallerSettings>();
            var jsonSerializer = context.Settings.Get<JsonSerializer>(WellKnownConfigurationKeys.SagaJsonSerializer);
            var readerCreator = context.Settings.Get<Func<TextReader, JsonReader>>(WellKnownConfigurationKeys.SagaReaderCreator);
            var writerCreator = context.Settings.Get<Func<TextWriter, JsonWriter>>(WellKnownConfigurationKeys.SagaWriterCreator);

            context.Services.AddSingleton<ISagaPersister>(provider => new AzureSagaPersister(provider.GetRequiredService<IProvideTableServiceClient>(),
                provider.GetRequiredService<TableCreator>(), conventionalTablePrefix, jsonSerializer, readerCreator, writerCreator));
        }
    }
}