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
    using Support;
    using Timeout.Core;
    using NUnit.Framework;

    static class TestHelper
    {
        const string EndpointName = "Sales";

        internal static TimeoutPersister CreateTimeoutPersister()
        {
            TimeoutPersister persister = null;
            try
            {
                persister = new TimeoutPersister(Testing.Utillities.GetEnvConfiguredConnectionStringForPersistence(),
                    AzureTimeoutStorageDefaults.TimeoutDataTableName, AzureTimeoutStorageDefaults.TimeoutManagerDataTableName,
                    AzureTimeoutStorageDefaults.TimeoutStateContainerName, 3600,
                    AzureTimeoutStorageDefaults.PartitionKeyScope, EndpointName, RuntimeEnvironment.MachineName);
            }
            catch (WebException exception)
            {
                // Azure blob container CreateIfNotExists() can falsely report HTTP 409 error, swallow it
                if (exception.Status != WebExceptionStatus.ProtocolError || (exception.Response is HttpWebResponse response && response.StatusCode != HttpStatusCode.NotFound))
                {
                    throw;
                }
            }
            return persister;
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

        internal static async Task PerformStorageCleanup()
        {
            await RemoveAllRowsForTable(AzureTimeoutStorageDefaults.TimeoutDataTableName);
            await RemoveAllRowsForTable(AzureTimeoutStorageDefaults.TimeoutManagerDataTableName);

            await RemoveAllBlobs();
        }

        static async Task RemoveAllRowsForTable(string tableName)
        {
            var cloudStorageAccount = CloudStorageAccount.Parse(Testing.Utillities.GetEnvConfiguredConnectionStringForPersistence());
            var table = cloudStorageAccount.CreateCloudTableClient().GetTableReference(tableName);

            await table.CreateIfNotExistsAsync();

            var projectionQuery = new TableQuery<DynamicTableEntity>().Select(new[]
            {
                "Destination"
            });

            // Define an entity resolver to work with the entity after retrieval.
            EntityResolver<Tuple<string, string>> resolver = (pk, rk, ts, props, etag) => props.ContainsKey("Destination") ? new Tuple<string, string>(pk, rk) : null;

            foreach (var tuple in await table.ExecuteQuerySegmentedAsync(
                query: projectionQuery,
                resolver: resolver,
                token: null))
            {
                var tableEntity = new DynamicTableEntity(tuple.Item1, tuple.Item2)
                {
                    ETag = "*"
                };
                await table.ExecuteAsync(TableOperation.Delete(tableEntity));
            }
        }

        static async Task RemoveAllBlobs()
        {
            var cloudStorageAccount = CloudStorageAccount.Parse(Testing.Utillities.GetEnvConfiguredConnectionStringForPersistence());
            var container = cloudStorageAccount.CreateCloudBlobClient().GetContainerReference("timeoutstate");
            await container.CreateIfNotExistsAsync();
            foreach (var blob in (await container.ListBlobsSegmentedAsync(null)).Results)
            {
                var cloudBlob = (ICloudBlob)blob;
                var requestOptions = new BlobRequestOptions
                {
                    RetryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(15), 5)
                };
                await cloudBlob.DeleteAsync(
                    deleteSnapshotsOption: DeleteSnapshotsOption.None,
                    accessCondition: AccessCondition.GenerateEmptyCondition(),
                    options: requestOptions,
                    operationContext: null);
            }
        }
    }
}