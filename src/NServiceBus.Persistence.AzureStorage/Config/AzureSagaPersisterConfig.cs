namespace NServiceBus.Config
{
    using System.Configuration;

    /// <summary>
    /// Config section for the Azure Saga Persister
    /// </summary>
    public class AzureSagaPersisterConfig : ConfigurationSection
    {

        [ObsoleteEx(
            TreatAsErrorFromVersion = "1",
            RemoveInVersion="7",
            Message = "Switch to the code API by Using `PersistenceExtentions<AzureStoragePersistence>.ConnectionString` instead.")]
        public string ConnectionString
        {
            get
            {
                return this["ConnectionString"] as string;
            }
            set
            {
                this["ConnectionString"] = value;
            }
        }

        /// <summary>
        /// Determines if the database should be auto updated
        /// </summary>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "1",
            RemoveInVersion="7",
            Message = "Switch to the code API by Using `PersistenceExtentions<AzureStoragePersistence>.CreateSchema` instead.")]
        public bool CreateSchema
        {
            get
            {
                return (bool)this["CreateSchema"];
            }
            set
            {
                this["CreateSchema"] = value;
            }
        }
    }
}
