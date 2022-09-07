namespace NServiceBus.TransactionalSession
{
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
            this PersistenceExtensions<AzureTablePersistence> persistenceExtensions)
        {
            persistenceExtensions.GetSettings().EnableFeatureByDefault<AzureTableTransactionalSession>();
            return persistenceExtensions;
        }
    }
}