namespace NServiceBus.Azure
{
    using System.Text.RegularExpressions;
    using System;
    using System.Collections.Generic;
    using System.Data.Services.Client;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web.Script.Serialization;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using Microsoft.WindowsAzure.Storage.Table;
    using NServiceBus.Extensibility;
    using Timeout.Core;
    using Timeout.TimeoutLogic;
    using System.Net;

    /// <summary>
    /// Provides that ability to save and retrieve timeout information
    /// </summary>
    public class TimeoutPersister : IPersistTimeouts, IQueryTimeouts
    {
        readonly string timeoutDataTableName;
        readonly string timeoutManagerDataTableName;
        readonly string timeoutStateContainerName;
        readonly int catchUpInterval;
        readonly string partitionKeyScope;
        readonly string endpointName;
        string sanitizedEndpointInstanceName;
        CloudTableClient client;
        CloudBlobClient cloudBlobclient;

        /// <summary>
        /// </summary>
        /// <param name="timeoutConnectionString">Connection string for the Azure table store</param>
        /// <param name="timeoutDataTableName">Name of the timeout data table</param>
        /// <param name="timeoutManagerDataTableName">Name of the timeout manager data table</param>
        /// <param name="timeoutStateContainerName">Name of the timeout state container</param>
        /// <param name="catchUpInterval">Amount of time in seconds to increment last successful read time by</param>
        /// <param name="partitionKeyScope">DateTime format to use in Partition Key</param>
        /// <param name="endpointName">Endpoint Name</param>
        /// <param name="hostDisplayName">Host Display Name</param>
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

        /// <summary>
        /// Retrieves the next range of timeouts that are due.
        /// </summary>
        /// <param name="startSlice">The time where to start retrieving the next slice, the slice should exclude this date.</param>
        /// <returns>Returns the next range of timeouts that are due.</returns>
        public async Task<TimeoutsChunk> GetNextChunk(DateTime startSlice)
        {
            var now = DateTime.UtcNow;

            var timeoutDataTable = client.GetTableReference(timeoutDataTableName);
            var timeoutManagerDataTable = client.GetTableReference(timeoutManagerDataTableName);

            var lastSuccessfulReadEntity = GetLastSuccessfulRead(timeoutManagerDataTable);
            var lastSuccessfulRead = lastSuccessfulReadEntity?.LastSuccessfullRead;

            IQueryable<TimeoutDataEntity> query;

            if (lastSuccessfulRead.HasValue)
            {
                query = from c in timeoutDataTable.CreateQuery<TimeoutDataEntity>()
                            where string.Compare(c.PartitionKey, lastSuccessfulRead.Value.ToString(partitionKeyScope), StringComparison.InvariantCultureIgnoreCase) != 0
                                && string.Compare(c.PartitionKey, now.ToString(partitionKeyScope),StringComparison.InvariantCultureIgnoreCase) != 0
                                && c.OwningTimeoutManager == endpointName
                        select c;
            }
            else
            {
                query = from c in timeoutDataTable.CreateQuery<TimeoutDataEntity>()
                        where c.OwningTimeoutManager == endpointName
                        select c;
            }

            var result = query
                .Take(1000)
                .ToList()
                .OrderBy(c => c.Time);

            var allTimeouts = result.ToList();
            if (allTimeouts.Count == 0)
            {
                return new TimeoutsChunk(new List<TimeoutsChunk.Timeout>(), now.AddSeconds(1));
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
            var nextTimeToRunQuery = lastSuccessfulRead.HasValue ? lastSuccessfulRead.Value
                                        : (future != null ? future.Time : now.AddSeconds(1));

            var timeoutsChunk = new TimeoutsChunk(
                pastTimeouts.Where(c => !string.IsNullOrEmpty(c.RowKey))
                    .Select(c => new TimeoutsChunk.Timeout(c.RowKey, c.Time))
                    .Distinct(new TimoutChunkComparer())
                    .ToList(),
                nextTimeToRunQuery);

            await UpdateSuccessfulRead(timeoutManagerDataTable, lastSuccessfulReadEntity).ConfigureAwait(false);
           
            return timeoutsChunk;
        }

        

        /// <summary>
        /// Add a new timeout entry
        /// </summary>
        /// <param name="timeout">The timeout to be added</param>
        /// <param name="context">The current pipeline context</param>
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

            var timeoutDataEntity = GetTimeoutData(timeoutDataTable, identifier, string.Empty);
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

        /// <summary>
        /// Peek at an existing timeout entry
        /// </summary>
        /// <param name="timeoutId">The ID of the timeout that is being requested</param>
        /// <param name="context">The current pipeline context</param>
        /// <returns>The requested timeout entry</returns>
        public Task<TimeoutData> Peek(string timeoutId, ContextBag context)
        {
            var timeoutDataTable = client.GetTableReference(timeoutDataTableName);

            var timeoutDataEntity = GetTimeoutData(timeoutDataTable, timeoutId, string.Empty);
            if (timeoutDataEntity == null)
            {
                return Task.FromResult<TimeoutData>(null);
            }

            var timeoutData = new TimeoutData
            {
                Destination = timeoutDataEntity.Destination, //TODO: check if the change here is causing issues
                SagaId = timeoutDataEntity.SagaId,
                State = Download(timeoutDataEntity.StateAddress).Result,
                Time = timeoutDataEntity.Time,
                Id = timeoutDataEntity.RowKey,
                OwningTimeoutManager = timeoutDataEntity.OwningTimeoutManager,
                Headers = Deserialize(timeoutDataEntity.Headers)
            };
            return Task.FromResult(timeoutData);
        }

        /// <summary>
        /// Safe method for removing a timeout entry
        /// </summary>
        /// <param name="timeoutId">ID of the timeout you want to try deleting</param>
        /// <param name="context">The current pipeline context</param>
        /// <returns>True/False indicating successful or unsucessful deletion</returns>
        public async Task<bool> TryRemove(string timeoutId, ContextBag context)
        {
            var timeoutDataTable = client.GetTableReference(timeoutDataTableName);

            var timeoutDataEntity = GetTimeoutData(timeoutDataTable, timeoutId, string.Empty);
            if (timeoutDataEntity == null)
            {
                return false;
            }

            var deleteTasks = new List<Task>
            {
                DeleteSagaEntity(timeoutId, timeoutDataTable, timeoutDataEntity),
                DeleteTimeEntity(timeoutDataTable, timeoutDataEntity.Time.ToString(partitionKeyScope), timeoutId),
                DeleteState(timeoutDataEntity.StateAddress)
            };

            try
            {
                await Task.WhenAll(deleteTasks).ConfigureAwait(false);
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

        /// <summary>
        /// Remove a single timeout entry
        /// </summary>
        /// <param name="sagaId">The saga ID used to find the timeout that will be removed</param>
        /// <param name="context">The current pipeline context</param>
        public async Task RemoveTimeoutBy(Guid sagaId, ContextBag context)
        {
            var timeoutDataTable = client.GetTableReference(timeoutDataTableName);

            var query = (from c in timeoutDataTable.CreateQuery<TimeoutDataEntity>()
                         where c.PartitionKey == sagaId.ToString()
                         select c);

            foreach (var timeoutDataEntityBySaga in query.Take(1000))
            {
                await DeleteState(timeoutDataEntityBySaga.StateAddress).ConfigureAwait(false);
                await DeleteTimeEntity(timeoutDataTable, timeoutDataEntityBySaga.Time.ToString(partitionKeyScope), timeoutDataEntityBySaga.RowKey).ConfigureAwait(false);
                await DeleteMainEntity(timeoutDataTable, timeoutDataEntityBySaga.RowKey, string.Empty).ConfigureAwait(false);
                await DeleteSagaEntity(timeoutDataTable, timeoutDataEntityBySaga).ConfigureAwait(false);
            }
        }

        Task DeleteMainEntity(CloudTable timeoutDataTable, string partitionKey, string rowKey)
        {
            var timeoutDataEntity = GetTimeoutData(timeoutDataTable, partitionKey, rowKey);

            if (timeoutDataEntity != null)
            {
                return DeleteMainEntity(timeoutDataEntity, timeoutDataTable);
            }
            return TaskEx.CompletedTask;
        }

        Task DeleteMainEntity(TimeoutDataEntity timeoutDataEntity, CloudTable timeoutDataTable)
        {
            var deleteOperation = TableOperation.Delete(timeoutDataEntity);
            return timeoutDataTable.ExecuteAsync(deleteOperation);
        }

        Task DeleteTimeEntity(CloudTable timeoutDataTable, string partitionKey, string rowKey)
        {
            var timeoutDataEntityByTime = GetTimeoutData(timeoutDataTable, partitionKey, rowKey);
            if (timeoutDataEntityByTime != null)
            {
                var deleteByTimeOperation = TableOperation.Delete(timeoutDataEntityByTime);
                return timeoutDataTable.ExecuteAsync(deleteByTimeOperation);
            }

            return TaskEx.CompletedTask;
        }

        Task DeleteSagaEntity(string timeoutId, CloudTable timeoutDataTable, TimeoutDataEntity timeoutDataEntity)
        {
            var timeoutDataEntityBySaga = GetTimeoutData(timeoutDataTable, timeoutDataEntity.SagaId.ToString(), timeoutId);
            if (timeoutDataEntityBySaga != null)
            {
                return DeleteSagaEntity(timeoutDataTable, timeoutDataEntityBySaga);
            }
            return TaskEx.CompletedTask;
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

        Task SaveSagaEntry(TimeoutData timeout, CloudTable timeoutDataTable, string identifier, string headers)
        {
            var timeoutDataEntity = GetTimeoutData(timeoutDataTable, timeout.SagaId.ToString(), identifier);
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
                return timeoutDataTable.ExecuteAsync(addOperation);
            }
            return TaskEx.CompletedTask;
        }

        Task SaveTimeoutEntry(TimeoutData timeout, CloudTable timeoutDataTable, string identifier, string headers)
        {
            var timeoutDataEntity = GetTimeoutData(timeoutDataTable, timeout.Time.ToString(partitionKeyScope), identifier);

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
                return timeoutDataTable.ExecuteAsync(addOperation);
            }
            return TaskEx.CompletedTask;
        }

        TimeoutDataEntity GetTimeoutData(CloudTable timeoutDataTable, string partitionKey, string rowKey)
        {
             var timeoutDataEntity = (from c in timeoutDataTable.CreateQuery<TimeoutDataEntity>()
                where c.PartitionKey == partitionKey && c.RowKey == rowKey // issue #191 cannot occur when both partitionkey and rowkey are specified
                select c).ToList().SafeFirstOrDefault();
            return timeoutDataEntity;
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

                var buffer = new byte[16*1024];
                using (var ms = new MemoryStream())
                {
                    int read;
                    while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        ms.Write(buffer, 0, read);
                    }
                    return ms.ToArray();
                }
            }
        }

        string Serialize(Dictionary<string, string> headers)
        {
            var serializer = new JavaScriptSerializer();
            var result = serializer.Serialize(headers);
            return result;
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
            var n = rgx.Replace(s, "");
            return n;
        }

        TimeoutManagerDataEntity GetLastSuccessfulRead(CloudTable timeoutManagerDataTable)
        {

            var query = from m in timeoutManagerDataTable.CreateQuery<TimeoutManagerDataEntity>()
                        where m.PartitionKey == sanitizedEndpointInstanceName
                        select m;

            return query.SafeFirstOrDefault();
        }

        Task UpdateSuccessfulRead(CloudTable table, TimeoutManagerDataEntity read)
        {
            try
            {
                if (read == null)
                {
                    read = new TimeoutManagerDataEntity(sanitizedEndpointInstanceName, string.Empty)
                           {
                               LastSuccessfullRead = DateTime.UtcNow
                           };

                    var addOperation = TableOperation.Insert(read);
                    return table.ExecuteAsync(addOperation);
                }
                else
                {
                    var updateOperation = TableOperation.Replace(read);
                    return table.ExecuteAsync(updateOperation);
                }
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
    }
}
