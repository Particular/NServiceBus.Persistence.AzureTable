﻿namespace NServiceBus.Persistence.AzureTable
{
    static class WellKnownConfigurationKeys
    {
        public const string SagaStorageConventionalTablePrefix = "AzureSagaStorage.ConventionalTablePrefix";
        public const string SagaJsonSerializer = "AzureSagaStorage.JsonSerializerSettings";
        public const string SagaReaderCreator = "AzureSagaStorage.ReaderCreator";
        public const string SagaWriterCreator = "AzureSagaStorage.WriterCreator";

        public const string SubscriptionStorageTableName = "AzureSubscriptionStorage.TableName";
        public const string SubscriptionStorageCacheFor = "AzureSubscriptionStorage.CacheFor";
    }
}