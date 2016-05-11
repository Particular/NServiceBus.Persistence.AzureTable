namespace NServiceBus.Config
{
    using System.Configuration;

    public class AzureSubscriptionStorageConfig : ConfigurationSection
    {
        [ObsoleteEx(
            TreatAsErrorFromVersion = "1",
            RemoveInVersion="2",
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

        [ObsoleteEx(
            TreatAsErrorFromVersion = "1",
            RemoveInVersion="2",
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

        [ObsoleteEx(
            TreatAsErrorFromVersion = "1",
            RemoveInVersion="2",
            Message = "Switch to the code API by Using `PersistenceExtentions<AzureStoragePersistence>.TableName` instead.")]
        public string TableName
        {
            get
            {
                return this["TableName"] as string;
            }
            set
            {
                this["TableName"] = value;
            }
        }
    }
}