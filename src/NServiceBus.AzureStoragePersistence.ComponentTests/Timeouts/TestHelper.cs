namespace NServiceBus.AzureStoragePersistence.ComponentTests.Timeouts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using Microsoft.WindowsAzure.Storage.Table;
    using NServiceBus.Azure;
    using NServiceBus.Config;
    using NServiceBus.Support;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;

    static class TestHelper
    {
        const string EndpointName = "Sales";

        internal static TimeoutPersister CreateTimeoutPersister()
        {
            TimeoutPersister persister = null;
            try
            {
                var azureTimeoutPersisterConfig = new AzureTimeoutPersisterConfig();

                persister = new TimeoutPersister(AzurePersistenceTests.GetConnectionString(),
                    azureTimeoutPersisterConfig.TimeoutDataTableName, azureTimeoutPersisterConfig.TimeoutManagerDataTableName,
                    azureTimeoutPersisterConfig.TimeoutStateContainerName, 3600,
                    azureTimeoutPersisterConfig.PartitionKeyScope, EndpointName, RuntimeEnvironment.MachineName);
            }
            catch (WebException exception)
            {
                // Azure blob container CreateIfNotExists() can falsely report HTTP 409 error, swallow it
                if (exception.Status != WebExceptionStatus.ProtocolError || (exception.Response is HttpWebResponse && ((HttpWebResponse) exception.Response).StatusCode != HttpStatusCode.NotFound))
                {
                    throw;
                }
            }
            return persister;
        }

        public static CloudBlockBlob CreateTimeoutCloudBlockBlob(string timeoutBlobId)
        {
            var azureTimeoutPersisterConfig = new AzureTimeoutPersisterConfig();

            var account = CloudStorageAccount.Parse(AzurePersistenceTests.GetConnectionString());
            var client = account.CreateCloudTableClient();
            client.DefaultRequestOptions = new TableRequestOptions
            {
                RetryPolicy = new ExponentialRetry()
            };

            var cloudBlobclient = account.CreateCloudBlobClient();
            var container = cloudBlobclient.GetContainerReference(azureTimeoutPersisterConfig.TimeoutStateContainerName);

            return container.GetBlockBlobReference(timeoutBlobId);
        }

        internal static TimeoutData GenerateTimeoutWithHeaders()
        {
            return new TimeoutData
            {
                Time = DateTime.UtcNow.AddYears(-1),
                Destination = "address://some_azure_connection_string",
                SagaId = Guid.NewGuid(),
                State = new byte[]
                {
                    1,
                    2,
                    3,
                    4
                },
                Headers = new Dictionary<string, string>
                {
                    {"Prop1", "1234"},
                    {"Prop2", "text"}
                },
                OwningTimeoutManager = EndpointName
            };
        }

        internal static TimeoutData GenerateTimeoutWithSagaId(Guid sagaId)
        {
            var timeoutWithHeaders1 = GenerateTimeoutWithHeaders();
            timeoutWithHeaders1.SagaId = sagaId;
            return timeoutWithHeaders1;
        }

        internal static async Task<List<Tuple<string, DateTime>>> GetAllTimeoutsUsingGetNextChunk(TimeoutPersister persister)
        {
            var timeouts = await persister.GetNextChunk(DateTime.Now.AddYears(-3));

            return timeouts.DueTimeouts.Select(timeout => new Tuple<string, DateTime>(timeout.Id, timeout.DueTime)).ToList();
        }

        public static async Task AssertAllTimeoutsThatHaveBeenRemoved(TimeoutPersister timeoutPersister)
        {
            var timeouts = await timeoutPersister.GetNextChunk(DateTime.Now.AddYears(-3));
            Assert.IsFalse(timeouts.DueTimeouts.Any());
        }

        internal static void PerformStorageCleanup()
        {
            RemoveAllRowsForTable(new AzureTimeoutPersisterConfig().TimeoutDataTableName);
            RemoveAllRowsForTable(new AzureTimeoutPersisterConfig().TimeoutManagerDataTableName);

            RemoveAllBlobs();
        }

        private static void RemoveAllRowsForTable(string tableName)
        {
            var cloudStorageAccount = CloudStorageAccount.Parse(AzurePersistenceTests.GetConnectionString());
            var table = cloudStorageAccount.CreateCloudTableClient().GetTableReference(tableName);

            table.CreateIfNotExists();

            var projectionQuery = new TableQuery<DynamicTableEntity>().Select(new[]
            {
                "Destination"
            });

            // Define an entity resolver to work with the entity after retrieval.
            EntityResolver<Tuple<string, string>> resolver = (pk, rk, ts, props, etag) => props.ContainsKey("Destination") ? new Tuple<string, string>(pk, rk) : null;

            foreach (var tuple in table.ExecuteQuery(projectionQuery, resolver))
            {
                var tableEntity = new DynamicTableEntity(tuple.Item1, tuple.Item2)
                {
                    ETag = "*"
                };
                table.Execute(TableOperation.Delete(tableEntity));
            }
        }

        private static void RemoveAllBlobs()
        {
            var cloudStorageAccount = CloudStorageAccount.Parse(AzurePersistenceTests.GetConnectionString());
            var container = cloudStorageAccount.CreateCloudBlobClient().GetContainerReference("timeoutstate");
            container.CreateIfNotExists();
            foreach (var blob in container.ListBlobs())
            {
                ((ICloudBlob) blob).Delete(DeleteSnapshotsOption.None, AccessCondition.GenerateEmptyCondition(), new BlobRequestOptions
                {
                    RetryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(15), 5)
                });
            }
        }
    }
}