namespace NServiceBus.Config
{
    using System.Configuration;

    /// <summary>
    /// Config section for the Azure Saga Persister
    /// </summary>
    public class AzureSagaPersisterConfig:ConfigurationSection
    {
        public AzureSagaPersisterConfig()
        {
            var defaultConnectionString = ConfigurationManager.AppSettings["NServiceBus/Persistence"];

            Properties.Add(new ConfigurationProperty("ConnectionString", typeof(string), defaultConnectionString,
                null, new CallbackValidator(typeof(string), AzureStorageSagaGuard.CheckConnectionString), ConfigurationPropertyOptions.None));

            Properties.Add(new ConfigurationProperty("CreateSchema", typeof(bool), AzureStorageSagaDefaults.CreateSchema,
                ConfigurationPropertyOptions.None));

        }

        /// <summary>
        /// Connectionstring
        /// </summary>
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
        /// ´Determines if the database should be auto updated
        /// </summary>
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