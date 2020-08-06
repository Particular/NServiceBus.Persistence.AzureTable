namespace NServiceBus.PersistenceTesting
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Outbox;
    using NServiceBus.Sagas;
    using Persistence;
    using Persistence.AzureStorage;

    public partial class PersistenceTestsConfiguration
    {
        public bool SupportsDtc => false;

        public bool SupportsOutbox => false;

        public bool SupportsFinders => false;

        public bool SupportsPessimisticConcurrency => false;

        public ISagaIdGenerator SagaIdGenerator { get; set; }

        public ISagaPersister SagaStorage { get; set; }

        public ISynchronizedStorage SynchronizedStorage { get; set; }

        public ISynchronizedStorageAdapter SynchronizedStorageAdapter { get; set; }

        public IOutboxStorage OutboxStorage { get; }

        public Task Configure()
        {
            var connectionString = GetEnvConfiguredConnectionStringForPersistence();
            SagaIdGenerator = new DefaultSagaIdGenerator();
            SagaStorage = new AzureSagaPersister(connectionString, true);
            SynchronizedStorage = new NoOpSynchronizedStorage();
            SynchronizedStorageAdapter = new NoOpSynchronizedStorageAdapter();
            return Task.CompletedTask;
        }

        public Task Cleanup()
        {
            return Task.CompletedTask;
        }

        static string GetEnvConfiguredConnectionStringForPersistence()
        {
            var environmentVartiableName = "AzureStoragePersistence_ConnectionString";
            var connectionString = GetEnvironmentVariable(environmentVartiableName);
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception($"Oh no! We couldn't find an environment variable '{environmentVartiableName}' with Azure Storage connection string.");
            }

            return connectionString;
        }

        static string GetEnvironmentVariable(string variable)
        {
            var candidate = Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.User);
            return string.IsNullOrWhiteSpace(candidate) ? Environment.GetEnvironmentVariable(variable) : candidate;
        }
    }
}