namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Timeouts
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
    using Timeout.Core;
    using NUnit.Framework;
    using Timeout.TimeoutLogic;

    static class TestHelper
    {
        public const string EndpointName = "Sales";

        internal static TimeoutPersister CreateTimeoutPersister()
        {
            TimeoutPersister persister = null;
            try
            {
                persister = new TimeoutPersister(AzurePersistenceTests.GetConnectionString(),
                    AzureTimeoutStorageDefaults.TimeoutDataTableName, AzureTimeoutStorageDefaults.TimeoutStateContainerName,
                    AzureTimeoutStorageDefaults.PartitionKeyScope, EndpointName);
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
            var account = CloudStorageAccount.Parse(AzurePersistenceTests.GetConnectionString());
            var client = account.CreateCloudTableClient();
            client.DefaultRequestOptions = new TableRequestOptions
            {
                RetryPolicy = new ExponentialRetry()
            };

            var cloudBlobclient = account.CreateCloudBlobClient();
            var container = cloudBlobclient.GetContainerReference(AzureTimeoutStorageDefaults.TimeoutStateContainerName);

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

        internal static async Task<List<Tuple<string, DateTime>>> GetAllTimeoutsRaw()
        {
            var account = CloudStorageAccount.Parse(AzurePersistenceTests.GetConnectionString());
            var client = account.CreateCloudTableClient();
            var table = client.GetTableReference(AzureTimeoutStorageDefaults.TimeoutDataTableName);
            var entities = await table.ExecuteQueryAsync(new TableQuery<TimeoutDataEntity>());
            return entities.Where(c => !string.IsNullOrEmpty(c.RowKey))
                .Select(c => new TimeoutsChunk.Timeout(c.RowKey, c.Time))
                .Distinct(new TimoutChunkComparer())
                .Select(c => new Tuple<string, DateTime>(c.Id, c.DueTime))
                .ToList();
        }

        public static async Task AssertAllTimeoutsThatHaveBeenRemoved(TimeoutPersister timeoutPersister)
        {
            var timeouts = await timeoutPersister.GetNextChunk(DateTime.Now.AddYears(-3));
            Assert.IsFalse(timeouts.DueTimeouts.Any());
        }

        internal static Task PerformStorageCleanup()
        {
            return Task.WhenAll(
                RemoveAllRowsForTable(AzureTimeoutStorageDefaults.TimeoutDataTableName),
                RemoveAllBlobs());
        }

        static async Task RemoveAllRowsForTable(string tableName)
        {
            var cloudStorageAccount = CloudStorageAccount.Parse(AzurePersistenceTests.GetConnectionString());
            var table = cloudStorageAccount.CreateCloudTableClient().GetTableReference(tableName);

            await table.CreateIfNotExistsAsync();

            var entities = await table.ExecuteQueryAsync(new TableQuery<DynamicTableEntity>());

            await Task.WhenAll(entities.Select(e => table.ExecuteAsync(TableOperation.Delete(e))));
        }

        static async Task RemoveAllBlobs()
        {
            var cloudStorageAccount = CloudStorageAccount.Parse(AzurePersistenceTests.GetConnectionString());
            var container = cloudStorageAccount.CreateCloudBlobClient().GetContainerReference("timeoutstate");
            await container.CreateIfNotExistsAsync();
            var blobs = await container.ListBlobAsync();
            await Task.WhenAll(blobs.Select(GetDelete));
        }

        static Task GetDelete(object blob)
        {
            var options = new BlobRequestOptions
            {
                RetryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(15), 5)
            };
            return ((ICloudBlob)blob).DeleteAsync(DeleteSnapshotsOption.None, AccessCondition.GenerateEmptyCondition(), options, new OperationContext());
        }
    }
}