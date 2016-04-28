# NServiceBus.Persistence.AzureStorage
The official [NServiceBus](https://github.com/Particular/NServiceBus) persistence implementation for [Azure Storage](https://azure.microsoft.com/en-us/services/storage/).

Learn more about NServiceBus.Persistence.AzureStorage through our [documentation](http://docs.particular.net/nservicebus/azure-storage-persistence/).

If you are interested in contributing, please follow the instructions [here](https://github.com/Particular/NServiceBus/blob/develop/CONTRIBUTING.md).

## Before running the tests

The tests require a connection to Azure Storage. We recommend [creating an Azure Storage account](https://azure.microsoft.com/en-us/documentation/services/storage/) rather than using the Azure Storage Emulator.

After creating the Azure Storage account, locate the connection string for the account. You can find it by selecting the storage account in the Azure portal, selecting "Access keys", then clicking "View connection string" next to the key you wish to use. (Click the ellipsis next to the key if you don't see the option to view the connection string.)

Copy the connection string into an environment variable called `AzureStoragePersistence.ConnectionString` and into an environment variable called `AzureStorageQueueTransport.ConnectionString`.

## Maintainers
The following team is responsible for this repository: @Particular/azure-storage-persistence-maintainers
