using Azure;

namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Timeouts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using Microsoft.Azure.Cosmos.Table;
    using Support;
    using Timeout.Core;
    using NUnit.Framework;

    static class TestHelper
    {
        const string EndpointName = "Sales";

        internal static TimeoutPersister CreateTimeoutPersister(Func<DateTime> dateTimeNowUtcGenerator = null)
        {
            TimeoutPersister persister = null;
            try
            {
                persister = new TimeoutPersister(Testing.Utillities.GetEnvConfiguredConnectionStringForPersistence(),
                    AzureTimeoutStorageDefaults.TimeoutDataTableName, AzureTimeoutStorageDefaults.TimeoutManagerDataTableName,
                    AzureTimeoutStorageDefaults.TimeoutStateContainerName, 3600,
                    AzureTimeoutStorageDefaults.PartitionKeyScope, EndpointName, RuntimeEnvironment.MachineName,
                    dateTimeNowUtcGenerator ?? (() => DateTime.UtcNow));
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

            var runningQuery = new TableQuery<DynamicTableEntity>()
            {
                FilterString = projectionQuery.FilterString,
                SelectColumns = projectionQuery.SelectColumns
            };
            TableContinuationToken token = null;
            var operationCount = 0;
            do
            {
                runningQuery.TakeCount = projectionQuery.TakeCount - operationCount;

                var seg = await table.ExecuteQuerySegmentedAsync(runningQuery, token);
                token = seg.ContinuationToken;
                foreach (var entity in seg)
                {
                    var tableEntity = new DynamicTableEntity(entity.PartitionKey, entity.RowKey)
                    {
                        ETag = "*"
                    };
                    await table.ExecuteAsync(TableOperation.Delete(tableEntity));
                    operationCount++;
                }

            }
            while (token != null && (projectionQuery.TakeCount == null || operationCount < projectionQuery.TakeCount.Value));
        }

        static async Task RemoveAllBlobs()
        {
            // TODO: Check if this works
            var connectionString = Testing.Utillities.GetEnvConfiguredConnectionStringForPersistence();
            var blobContainerClient = new BlobContainerClient(connectionString, "timeoutstate");
            await blobContainerClient.DeleteIfExistsAsync();

            int attempt = 0;
            RequestFailedException exception;
            do
            {
                try
                {
                    await blobContainerClient.CreateIfNotExistsAsync();
                    exception = null;
                }
                catch (RequestFailedException e) when (e.Status == 409 || e.ErrorCode == "ContainerBeingDeleted")
                {
                    exception = e;
                    await Task.Delay(attempt++ * 1000);
                }
            }
            while (exception != null);
        }
    }
}