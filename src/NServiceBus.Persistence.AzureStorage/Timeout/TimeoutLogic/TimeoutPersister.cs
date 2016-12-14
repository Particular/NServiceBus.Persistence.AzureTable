namespace NServiceBus.Persistence.AzureStorage
{
    using System;
    using System.Collections.Generic;
    using System.Data.Services.Client;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Web.Script.Serialization;
    using Extensibility;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using Microsoft.WindowsAzure.Storage.Table;
    using Timeout.Core;
    using Timeout.TimeoutLogic;

    class TimeoutPersister : IPersistTimeouts, IQueryTimeouts
    {
        public TimeoutPersister(string timeoutConnectionString, string timeoutDataTableName, string timeoutManagerDataTableName, string timeoutStateContainerName, int catchUpInterval, string partitionKeyScope, string endpointName, string hostDisplayName)
        {
            this.timeoutDataTableName = timeoutDataTableName;
            this.timeoutManagerDataTableName = timeoutManagerDataTableName;
            this.timeoutStateContainerName = timeoutStateContainerName;
            this.catchUpInterval = catchUpInterval;
            this.partitionKeyScope = partitionKeyScope;
            this.endpointName = endpointName;

            // Unicast sets the default for this value to the machine name.
            // NServiceBus.Host.AzureCloudService, when running in a cloud environment, sets this value to the current RoleInstanceId.
            if (string.IsNullOrWhiteSpace(hostDisplayName))
            {
                throw new InvalidOperationException("The TimeoutPersister for Azure Storage Persistence requires a host-specific identifier to execute properly. Unable to find identifier in the `NServiceBus.HostInformation.DisplayName` settings key.");
            }

            sanitizedEndpointInstanceName = Sanitize(endpointName + "_" + hostDisplayName);

            var account = CloudStorageAccount.Parse(timeoutConnectionString);
            client = account.CreateCloudTableClient();
            client.DefaultRequestOptions = new TableRequestOptions
            {
                RetryPolicy = new ExponentialRetry()
            };

            cloudBlobclient = account.CreateCloudBlobClient();
        }

        public async Task Add(TimeoutData timeout, ContextBag context)
        {
            var timeoutDataTable = client.GetTableReference(timeoutDataTableName);

            string identifier;
            timeout.Headers.TryGetValue(Headers.MessageId, out identifier);
            if (string.IsNullOrEmpty(identifier))
            {
                identifier = Guid.NewGuid().ToString();
            }
            timeout.Id = identifier;

            var timeoutDataEntity = await GetTimeoutData(timeoutDataTable, identifier, string.Empty).ConfigureAwait(false);
            if (timeoutDataEntity != null) return;

            var headers = Serialize(timeout.Headers);

            var saveActions = new List<Task>
            {
                SaveCurrentTimeoutState(timeout.State, identifier),
                SaveTimeoutEntry(timeout, timeoutDataTable, identifier, headers),
                SaveSagaEntry(timeout, timeoutDataTable, identifier, headers)
            };

            await Task.WhenAll(saveActions).ConfigureAwait(false);
            await SaveMainEntry(timeout, identifier, headers, timeoutDataTable).ConfigureAwait(false);
        }

        public async Task<TimeoutData> Peek(string timeoutId, ContextBag context)
        {
            var timeoutDataTable = client.GetTableReference(timeoutDataTableName);

            var timeoutDataEntity = await GetTimeoutData(timeoutDataTable, timeoutId, string.Empty).ConfigureAwait(false);
            if (timeoutDataEntity == null)
            {
                return null;
            }

            var timeoutData = new TimeoutData
            {
                Destination = timeoutDataEntity.Destination, //TODO: check if the change here is causing issues
                SagaId = timeoutDataEntity.SagaId,
                State = await Download(timeoutDataEntity.StateAddress).ConfigureAwait(false),
                Time = timeoutDataEntity.Time,
                Id = timeoutDataEntity.RowKey,
                OwningTimeoutManager = timeoutDataEntity.OwningTimeoutManager,
                Headers = Deserialize(timeoutDataEntity.Headers)
            };
            return timeoutData;
        }

        public async Task<bool> TryRemove(string timeoutId, ContextBag context)
        {
            var timeoutDataTable = client.GetTableReference(timeoutDataTableName);

            var timeoutDataEntity = await GetTimeoutData(timeoutDataTable, timeoutId, string.Empty).ConfigureAwait(false);
            if (timeoutDataEntity == null)
            {
                return false;
            }

            try
            {
                await DeleteSagaEntity(timeoutId, timeoutDataTable, timeoutDataEntity).ConfigureAwait(false);
                await DeleteTimeEntity(timeoutDataTable, timeoutDataEntity.Time.ToString(partitionKeyScope), timeoutId).ConfigureAwait(false);
                await DeleteState(timeoutDataEntity.StateAddress).ConfigureAwait(false);
                await DeleteMainEntity(timeoutDataEntity, timeoutDataTable).ConfigureAwait(false);
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode != (int)HttpStatusCode.NotFound)
                {
                    throw;
                }

                return false;
            }

            return true;
        }

        public async Task RemoveTimeoutBy(Guid sagaId, ContextBag context)
        {
            var timeoutDataTable = client.GetTableReference(timeoutDataTableName);

            var query = new TableQuery<TimeoutDataEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sagaId.ToString()));

            var results = await timeoutDataTable.ExecuteQueryAsync(query, take: 1000).ConfigureAwait(false);

            foreach (var timeoutDataEntityBySaga in results)
            {
                await DeleteState(timeoutDataEntityBySaga.StateAddress).ConfigureAwait(false);
                await DeleteTimeEntity(timeoutDataTable, timeoutDataEntityBySaga.Time.ToString(partitionKeyScope), timeoutDataEntityBySaga.RowKey).ConfigureAwait(false);
                await DeleteMainEntity(timeoutDataTable, timeoutDataEntityBySaga.RowKey, string.Empty).ConfigureAwait(false);
                await DeleteSagaEntity(timeoutDataTable, timeoutDataEntityBySaga).ConfigureAwait(false);
            }
        }

        public async Task<TimeoutsChunk> GetNextChunk(DateTime startSlice)
        {
            var now = DateTime.UtcNow;

            var timeoutDataTable = client.GetTableReference(timeoutDataTableName);
            var timeoutManagerDataTable = client.GetTableReference(timeoutManagerDataTableName);

            await TryUpdateSuccessfulRead(timeoutManagerDataTable).ConfigureAwait(false);

            var lastSuccessfulReadEntity = await GetLastSuccessfulRead(timeoutManagerDataTable).ConfigureAwait(false);
            var lastSuccessfulRead = lastSuccessfulReadEntity?.LastSuccessfullRead;

            var floor = lastSuccessfulRead ?? DateTime.UtcNow.AddYears(-1);

            var query = new TableQuery<TimeoutDataEntity>()
                .Where(
                    TableQuery.CombineFilters(
                        TableQuery.CombineFilters(
                            TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.GreaterThanOrEqual, floor.ToString(partitionKeyScope)),
                            TableOperators.And,
                            TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.LessThanOrEqual, now.ToString(partitionKeyScope))),
                        TableOperators.And,
                        TableQuery.CombineFilters(
                            TableQuery.GenerateFilterCondition("OwningTimeoutManager", QueryComparisons.Equal, endpointName),
                            TableOperators.And,
                            TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.NotEqual, string.Empty) //Remove the non-date primary key records
                        )
                    )
                );

            var timeoutDataEntities = await timeoutDataTable.ExecuteQueryAsync(query, take: 1000).ConfigureAwait(false);
            var result = timeoutDataEntities.OrderBy(c => c.Time);

            var allTimeouts = result.ToList();
            if (allTimeouts.Count == 0)
            {
                return new TimeoutsChunk(new TimeoutsChunk.Timeout[0], now.AddSeconds(1));
            }

            var pastTimeouts = allTimeouts.Where(c => c.Time > startSlice && c.Time <= now).ToList();
            var futureTimeouts = allTimeouts.Where(c => c.Time > now).ToList();

            if (lastSuccessfulReadEntity != null)
            {
                var catchingUp = lastSuccessfulRead.Value.AddSeconds(catchUpInterval);
                lastSuccessfulRead = catchingUp > now ? now : catchingUp;
                lastSuccessfulReadEntity.LastSuccessfullRead = lastSuccessfulRead.Value;
            }

            var future = futureTimeouts.SafeFirstOrDefault();
            var nextTimeToRunQuery = lastSuccessfulRead ?? (future?.Time ?? now.AddSeconds(1));

            var timeoutsChunk = new TimeoutsChunk(
                pastTimeouts.Where(c => !string.IsNullOrEmpty(c.RowKey))
                    .Select(c => new TimeoutsChunk.Timeout(c.RowKey, c.Time))
                    .Distinct(new TimoutChunkComparer())
                    .ToArray(),
                nextTimeToRunQuery);

            updateSuccessfulReadOperationForNextSpin = GetUpdateSuccessfulRead(lastSuccessfulReadEntity);
            return timeoutsChunk;
        }

        async Task TryUpdateSuccessfulRead(CloudTable timeoutManagerDataTable)
        {
            if (updateSuccessfulReadOperationForNextSpin != null)
            {
                try
                {
                    await UpdateSuccessfulRead(timeoutManagerDataTable, updateSuccessfulReadOperationForNextSpin).ConfigureAwait(false);
                }
                finally
                {
                    updateSuccessfulReadOperationForNextSpin = null;
                }
            }
        }

        async Task DeleteMainEntity(CloudTable timeoutDataTable, string partitionKey, string rowKey)
        {
            var timeoutDataEntity = await GetTimeoutData(timeoutDataTable, partitionKey, rowKey).ConfigureAwait(false);

            if (timeoutDataEntity != null)
            {
                await DeleteMainEntity(timeoutDataEntity, timeoutDataTable).ConfigureAwait(false);
            }
        }

        Task DeleteMainEntity(TimeoutDataEntity timeoutDataEntity, CloudTable timeoutDataTable)
        {
            var deleteOperation = TableOperation.Delete(timeoutDataEntity);
            return timeoutDataTable.ExecuteAsync(deleteOperation);
        }

        async Task DeleteTimeEntity(CloudTable timeoutDataTable, string partitionKey, string rowKey)
        {
            var timeoutDataEntityByTime = await GetTimeoutData(timeoutDataTable, partitionKey, rowKey).ConfigureAwait(false);
            if (timeoutDataEntityByTime != null)
            {
                var deleteByTimeOperation = TableOperation.Delete(timeoutDataEntityByTime);
                await timeoutDataTable.ExecuteAsync(deleteByTimeOperation).ConfigureAwait(false);
            }
        }

        async Task DeleteSagaEntity(string timeoutId, CloudTable timeoutDataTable, TimeoutDataEntity timeoutDataEntity)
        {
            var timeoutDataEntityBySaga = await GetTimeoutData(timeoutDataTable, timeoutDataEntity.SagaId.ToString(), timeoutId).ConfigureAwait(false);
            if (timeoutDataEntityBySaga != null)
            {
                await DeleteSagaEntity(timeoutDataTable, timeoutDataEntityBySaga).ConfigureAwait(false);
            }
        }

        Task DeleteSagaEntity(CloudTable timeoutDataTable, TimeoutDataEntity sagaEntity)
        {
            var deleteSagaOperation = TableOperation.Delete(sagaEntity);
            return timeoutDataTable.ExecuteAsync(deleteSagaOperation);
        }

        Task SaveMainEntry(TimeoutData timeout, string identifier, string headers, CloudTable timeoutDataTable)
        {
            var timeoutDataObject = new TimeoutDataEntity(identifier, string.Empty)
            {
                Destination = timeout.Destination,
                SagaId = timeout.SagaId,
                StateAddress = identifier,
                Time = timeout.Time,
                OwningTimeoutManager = timeout.OwningTimeoutManager,
                Headers = headers
            };
            var addEntityOperation = TableOperation.Insert(timeoutDataObject);
            return timeoutDataTable.ExecuteAsync(addEntityOperation);
        }

        async Task SaveSagaEntry(TimeoutData timeout, CloudTable timeoutDataTable, string identifier, string headers)
        {
            var timeoutDataEntity = await GetTimeoutData(timeoutDataTable, timeout.SagaId.ToString(), identifier).ConfigureAwait(false);
            if (timeout.SagaId != default(Guid) && timeoutDataEntity == null)
            {
                var timeoutData = new TimeoutDataEntity(timeout.SagaId.ToString(), identifier)
                {
                    Destination = timeout.Destination,
                    SagaId = timeout.SagaId,
                    StateAddress = identifier,
                    Time = timeout.Time,
                    OwningTimeoutManager = timeout.OwningTimeoutManager,
                    Headers = headers
                };

                var addOperation = TableOperation.Insert(timeoutData);
                await timeoutDataTable.ExecuteAsync(addOperation).ConfigureAwait(false);
            }
        }

        async Task SaveTimeoutEntry(TimeoutData timeout, CloudTable timeoutDataTable, string identifier, string headers)
        {
            var timeoutDataEntity = await GetTimeoutData(timeoutDataTable, timeout.Time.ToString(partitionKeyScope), identifier).ConfigureAwait(false);

            if (timeoutDataEntity == null)
            {
                var timeoutData = new TimeoutDataEntity(timeout.Time.ToString(partitionKeyScope), identifier)
                {
                    Destination = timeout.Destination,
                    SagaId = timeout.SagaId,
                    StateAddress = identifier,
                    Time = timeout.Time,
                    OwningTimeoutManager = timeout.OwningTimeoutManager,
                    Headers = headers
                };
                var addOperation = TableOperation.Insert(timeoutData);
                await timeoutDataTable.ExecuteAsync(addOperation).ConfigureAwait(false);
            }
        }

        async Task<TimeoutDataEntity> GetTimeoutData(CloudTable timeoutDataTable, string partitionKey, string rowKey)
        {
            var retrieveOperation = TableOperation.Retrieve<TimeoutDataEntity>(partitionKey, rowKey);
            return (await timeoutDataTable.ExecuteAsync(retrieveOperation).ConfigureAwait(false)).Result as TimeoutDataEntity;
        }

        async Task SaveCurrentTimeoutState(byte[] state, string stateAddress)
        {
            var container = cloudBlobclient.GetContainerReference(timeoutStateContainerName);
            var blob = container.GetBlockBlobReference(stateAddress);
            using (var stream = new MemoryStream(state))
            {
                await blob.UploadFromStreamAsync(stream).ConfigureAwait(false);
            }
        }

        async Task<byte[]> Download(string stateAddress)
        {
            var container = cloudBlobclient.GetContainerReference(timeoutStateContainerName);

            var blob = container.GetBlockBlobReference(stateAddress);
            using (var stream = new MemoryStream())
            {
                await blob.DownloadToStreamAsync(stream).ConfigureAwait(false);
                stream.Position = 0;

                var buffer = new byte[16 * 1024];
                using (var ms = new MemoryStream())
                {
                    int read;
                    while ((read = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                    {
                        await ms.WriteAsync(buffer, 0, read).ConfigureAwait(false);
                    }
                    return ms.ToArray();
                }
            }
        }

        string Serialize(Dictionary<string, string> headers)
        {
            var serializer = new JavaScriptSerializer();
            return serializer.Serialize(headers);
        }

        Dictionary<string, string> Deserialize(string state)
        {
            if (string.IsNullOrEmpty(state))
            {
                return new Dictionary<string, string>();
            }

            var serializer = new JavaScriptSerializer();
            return serializer.Deserialize<Dictionary<string, string>>(state);
        }

        Task DeleteState(string stateAddress)
        {
            var container = cloudBlobclient.GetContainerReference(timeoutStateContainerName);
            var blob = container.GetBlockBlobReference(stateAddress);
            return blob.DeleteIfExistsAsync();
        }

        string Sanitize(string s)
        {
            var rgx = new Regex(@"[^a-zA-Z0-9\-_]");
            return rgx.Replace(s, "");
        }

        async Task<TimeoutManagerDataEntity> GetLastSuccessfulRead(CloudTable timeoutManagerDataTable)
        {
            var query = new TableQuery<TimeoutManagerDataEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sanitizedEndpointInstanceName));

            var results = await timeoutManagerDataTable.ExecuteQueryAsync(query, take: 1).ConfigureAwait(false);

            return results.SafeFirstOrDefault();
        }

        static Task UpdateSuccessfulRead(CloudTable table, TableOperation operation)
        {
            try
            {
                return table.ExecuteAsync(operation);
            }
            catch (DataServiceRequestException ex) // handle concurrency issues
            {
                var response = ex.Response.FirstOrDefault();
                //Concurrency Exception - PreCondition Failed or Entity Already Exists
                if (response != null && (response.StatusCode == 412 || response.StatusCode == 409))
                {
                    return TaskEx.CompletedTask;
                    // I assume we can ignore this condition?
                    // Time between read and update is very small, meaning that another instance has sent
                    // the timeout messages that this node intended to send and if not we will resend
                    // anything after the other node's last read value anyway on next request.
                }

                throw;
            }
        }

        TableOperation GetUpdateSuccessfulRead(TimeoutManagerDataEntity read)
        {
            if (read == null)
            {
                read = new TimeoutManagerDataEntity(sanitizedEndpointInstanceName, string.Empty)
                {
                    LastSuccessfullRead = DateTime.UtcNow
                };

                return TableOperation.Insert(read);
            }

            var updated = new TimeoutManagerDataEntity(sanitizedEndpointInstanceName, string.Empty)
            {
                ETag = read.ETag,
                Timestamp = read.Timestamp,
                LastSuccessfullRead = read.LastSuccessfullRead
            };

            return TableOperation.Replace(updated);
        }

        string timeoutDataTableName;
        string timeoutManagerDataTableName;
        string timeoutStateContainerName;
        int catchUpInterval;
        string partitionKeyScope;
        string endpointName;
        string sanitizedEndpointInstanceName;
        CloudTableClient client;
        CloudBlobClient cloudBlobclient;
        TableOperation updateSuccessfulReadOperationForNextSpin;
    }
}