namespace NServiceBus.Config
{
    using System.Configuration;

    public class AzureTimeoutPersisterConfig : ConfigurationSection
    {

        [ObsoleteEx(
            TreatAsErrorFromVersion = "1",
            RemoveInVersion="2",
            Message = "Switch to the code API by Using `PersistenceExtentions<AzureStoragePersistence>.ConnectionString` instead.")]
        public string ConnectionString
        {
            get { return (string)this["ConnectionString"]; }
            set { this["ConnectionString"] = value; }
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "1",
            RemoveInVersion="2",
            Message = "Switch to the code API by Using `PersistenceExtentions<AzureStoragePersistence>.TimeoutManagerDataTableName` instead.")]
        public string TimeoutManagerDataTableName
        {
            get { return (string)this["TimeoutManagerDataTableName"]; }
            set { this["TimeoutManagerDataTableName"] = value; }
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "1",
            RemoveInVersion="2",
            Message = "Switch to the code API by Using `PersistenceExtentions<AzureStoragePersistence>.TimeoutDataTableName` instead.")]
        public string TimeoutDataTableName
        {
            get { return (string)this["TimeoutDataTableName"]; }
            set { this["TimeoutDataTableName"] = value; }
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "1",
            RemoveInVersion="2",
            Message = "Switch to the code API by Using `PersistenceExtentions<AzureStoragePersistence>.CatchUpInterval` instead.")]
        public int CatchUpInterval
        {
            get { return (int)this["CatchUpInterval"]; }
            set { this["CatchUpInterval"] = value; }
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "1",
            RemoveInVersion="2",
            Message = "Switch to the code API by Using `PersistenceExtentions<AzureStoragePersistence>.ConnectionString` instead.")]
        public string PartitionKeyScope
        {
            get { return (string)this["PartitionKeyScope"]; }
            set { this["PartitionKeyScope"] = value; }
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "1",
            RemoveInVersion="2",
            Message = "Switch to the code API by Using `PersistenceExtentions<AzureStoragePersistence>.CreateSchema` instead.")]
        public bool CreateSchema
        {
            get { return (bool)this["CreateSchema"]; }
            set { this["CreateSchema"] = value; }
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "1",
            RemoveInVersion="2",
            Message = "Switch to the code API by Using `PersistenceExtentions<AzureStoragePersistence>.TimeoutStateContainerName` instead.")]
        public string TimeoutStateContainerName
        {
            get { return (string)this["TimeoutStateContainerName"]; }
            set { this["TimeoutStateContainerName"] = value; }
        }
    }
}