# NServiceBus.Persistence.AzureStorage
The official [NServiceBus](https://github.com/Particular/NServiceBus) persistence implementation for [Azure Table Storage](https://azure.microsoft.com/en-us/services/storage/tables/) and [Azure Cosmos DB Table API](https://docs.microsoft.com/en-us/azure/cosmos-db/table-support/).

Learn more about NServiceBus.Persistence.AzureStorage through our [documentation](http://docs.particular.net/nservicebus/azure-storage-persistence/).

If you are interested in contributing, please follow the instructions [here](https://github.com/Particular/NServiceBus/blob/develop/CONTRIBUTING.md).

## Before running the tests

The tests require a connection to Azure Table Storage and Cosmos Table API. We recommend [creating an Azure Storage account](https://azure.microsoft.com/en-us/documentation/services/storage/) and an Azure Cosmos DB instance using the Table API rather than using the Azure Storage or the Cosmos DB Emulator.

After creating the Azure Storage account, locate the connection string for the account. You can find it by selecting the storage account in the Azure portal, selecting "Access keys", then clicking "View connection string" next to the key you wish to use. (Click the ellipsis next to the key if you don't see the option to view the connection string.)

Copy the connection string into an environment variable called `AzureTable_StorageTable_ConnectionString`. 

After creating the Azure Cosmos DB Table API account, locate the connection string for the account. You can find it by selecting the Azure Cosmos DB instance, selecting "Connection String", then copy the primary or secondary connection string.

Copy the connection string into an environment variable called `AzureTable_CosmosDB_ConnectionString`. 
