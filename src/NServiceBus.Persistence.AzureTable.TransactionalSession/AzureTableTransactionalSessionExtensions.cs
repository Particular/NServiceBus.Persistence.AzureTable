namespace NServiceBus.TransactionalSession
{
    using System;
    using Configuration.AdvancedExtensibility;
    using Features;

    /// <summary>
    /// Enables the transactional session feature.
    /// </summary>
    public static class AzureTableTransactionalSessionExtensions
    {
        /// <summary>
        /// Enables transactional session for this endpoint.
        /// </summary>
        public static PersistenceExtensions<AzureTablePersistence> EnableTransactionalSession(
            this PersistenceExtensions<AzureTablePersistence> persistenceExtensions) =>
            EnableTransactionalSession(persistenceExtensions, new TransactionalSessionOptions());

        /// <summary>
        /// Enables the transactional session for this endpoint using the specified TransactionalSessionOptions.
        /// </summary>
        public static PersistenceExtensions<AzureTablePersistence> EnableTransactionalSession(this PersistenceExtensions<AzureTablePersistence> persistenceExtensions,
            TransactionalSessionOptions transactionalSessionOptions)
        {
            ArgumentNullException.ThrowIfNull(persistenceExtensions);
            ArgumentNullException.ThrowIfNull(transactionalSessionOptions);

            var settings = persistenceExtensions.GetSettings();

            settings.Set(transactionalSessionOptions);
            settings.EnableFeatureByDefault<AzureTableTransactionalSession>();

            return persistenceExtensions;
        }
    }
}