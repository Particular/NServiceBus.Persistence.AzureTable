We looked at several solutions like

- Backing up and restoring Azure Tables
- Exporting and reimporting Data with Azure Data Explorer
- How the data exporter tools of Microsoft handle the import and export

We ended deciding that the most important part is to make sure the data format that is written to the storage mimics the Release 2.x data format. So this folder contains the most relevant helpers tools from the 2.x version of the persistence plus some entity types that make sure the same entity format will be written to the storage.