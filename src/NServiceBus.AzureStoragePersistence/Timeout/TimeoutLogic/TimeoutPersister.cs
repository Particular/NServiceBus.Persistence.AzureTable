namespace NServiceBus.Azure
{
    using System.Text.RegularExpressions;
    using System;
    using System.Collections.Generic;
    using System.Data.Services.Client;
    using System.IO;
    using System.Linq;
    using System.Web.Script.Serialization;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using Microsoft.WindowsAzure.Storage.Table;
    using Timeout.Core;
    
    /// <summary>
    /// Provides that ability to save and retrieve timeout information
    /// </summary>
    public class TimeoutPersister : IPersistTimeouts, IPersistTimeoutsV2, IDetermineWhoCanSend
    {
        readonly string timeoutDataTableName;
        readonly string timeoutManagerDataTableName;
        readonly string timeoutStateContainerName;
        readonly int catchUpInterval;
        readonly string partitionKeyScope;
        readonly string endpointName;
        string sanitizedEndpointInstanceName;

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
        /// 
        /// </summary>
        /// <param name="startSlice">Time to start pulling the chunks at</param>
        /// <param name="nextTimeToRunQuery">Returns the next time that the GetNextChunk method should be called at</param>
        /// <returns>Collection of timeouts</returns>
        public IEnumerable<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            var results = new List<Tuple<string, DateTime>>();
           
            var now = DateTime.UtcNow;


            var timeoutDataTable = client.GetTableReference(timeoutDataTableName);
            var timeoutManagerDataTable = client.GetTableReference(timeoutManagerDataTableName);

            TimeoutManagerDataEntity lastSuccessfulReadEntity;
            var lastSuccessfulRead = TryGetLastSuccessfulRead(timeoutManagerDataTable, out lastSuccessfulReadEntity)
                                            ? lastSuccessfulReadEntity.LastSuccessfullRead
                                            : default(DateTime?);

            IQueryable<TimeoutDataEntity> query;

            if (lastSuccessfulRead.HasValue)
            {
                query = from c in timeoutDataTable.CreateQuery<TimeoutDataEntity>()
                            where c.PartitionKey.CompareTo(lastSuccessfulRead.Value.ToString(partitionKeyScope)) >= 0
                            && c.PartitionKey.CompareTo(now.ToString(partitionKeyScope)) <= 0
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
                nextTimeToRunQuery = now.AddSeconds(1);
                return results;
            }

            var pastTimeouts = allTimeouts.Where(c => c.Time > startSlice && c.Time <= now).ToList();
            var futureTimeouts = allTimeouts.Where(c => c.Time > now).ToList();

            if (lastSuccessfulReadEntity != null && lastSuccessfulRead.HasValue)
            {
                var catchingUp = lastSuccessfulRead.Value.AddSeconds(catchUpInterval);
                lastSuccessfulRead = catchingUp > now ? now : catchingUp;
                lastSuccessfulReadEntity.LastSuccessfullRead = lastSuccessfulRead.Value;
            }

            var future = futureTimeouts.SafeFirstOrDefault();
            nextTimeToRunQuery = lastSuccessfulRead.HasValue ? lastSuccessfulRead.Value
                                        : (future != null ? future.Time : now.AddSeconds(1));
                
            results = pastTimeouts
                .Where(c => !string.IsNullOrEmpty(c.RowKey))
                .Select(c => new Tuple<String, DateTime>(c.RowKey, c.Time))
                .Distinct()
                .ToList();

            UpdateSuccessfulRead(timeoutManagerDataTable, lastSuccessfulReadEntity);
           
            return results;
        }

        /// <summary>
        /// Add a new timeout entry
        /// </summary>
        /// <param name="timeout">The timeout to be added</param>
        public void Add(TimeoutData timeout)
        {
            var timeoutDataTable = client.GetTableReference(timeoutDataTableName);

            string identifier;
            timeout.Headers.TryGetValue(Headers.MessageId, out identifier);
            if (string.IsNullOrEmpty(identifier))
            {
                identifier = Guid.NewGuid().ToString();
            }

            TimeoutDataEntity timeoutDataEntity;
            if (TryGetTimeoutData(timeoutDataTable, identifier, string.Empty, out timeoutDataEntity)) return;

            Upload(timeout.State, identifier);
            var headers = Serialize(timeout.Headers);

            if (!TryGetTimeoutData(timeoutDataTable, timeout.Time.ToString(partitionKeyScope), identifier, out timeoutDataEntity))
            {
                var timeoutData = new TimeoutDataEntity(timeout.Time.ToString(partitionKeyScope), identifier)
                                    {
                                        Destination = timeout.Destination.ToString(),
                                        SagaId = timeout.SagaId,
                                        StateAddress = identifier,
                                        Time = timeout.Time,
                                        OwningTimeoutManager = timeout.OwningTimeoutManager,
                                        Headers = headers
                                    };
                var addOperation = TableOperation.Insert(timeoutData);
                timeoutDataTable.Execute(addOperation);
            }
            timeout.Id = identifier;

            if (timeout.SagaId != default(Guid) && !TryGetTimeoutData(timeoutDataTable, timeout.SagaId.ToString(), identifier, out timeoutDataEntity))
            {
                var timeoutData = new TimeoutDataEntity(timeout.SagaId.ToString(), identifier)
                                    {
                                        Destination = timeout.Destination.ToString(),
                                        SagaId = timeout.SagaId,
                                        StateAddress = identifier,
                                        Time = timeout.Time,
                                        OwningTimeoutManager = timeout.OwningTimeoutManager,
                                        Headers = headers
                                    };

                var addOperation = TableOperation.Insert(timeoutData);
                timeoutDataTable.Execute(addOperation);
            }

            var timeoutDataObject = new TimeoutDataEntity(identifier, string.Empty)
                                {
                                    Destination = timeout.Destination.ToString(),
                                    SagaId = timeout.SagaId,
                                    StateAddress = identifier,
                                    Time = timeout.Time,
                                    OwningTimeoutManager = timeout.OwningTimeoutManager,
                                    Headers = headers
                                };
            var addEntityOperation = TableOperation.Insert(timeoutDataObject);
            timeoutDataTable.Execute(addEntityOperation);
        }

        /// <summary>
        /// Peek at an existing timeout entry
        /// </summary>
        /// <param name="timeoutId">The ID of the timeout that is being requested</param>
        /// <returns>The requested timeout entry</returns>
        public TimeoutData Peek(string timeoutId)
        {
            var timeoutDataTable = client.GetTableReference(timeoutDataTableName);

            TimeoutDataEntity timeoutDataEntity;
            if (!TryGetTimeoutData(timeoutDataTable, timeoutId, string.Empty, out timeoutDataEntity))
            {
                return null;
            }

            var timeoutData = new TimeoutData
            {
                Destination = Address.Parse(timeoutDataEntity.Destination),
                SagaId = timeoutDataEntity.SagaId,
                State = Download(timeoutDataEntity.StateAddress),
                Time = timeoutDataEntity.Time,
                Id = timeoutDataEntity.RowKey,
                OwningTimeoutManager = timeoutDataEntity.OwningTimeoutManager,
                Headers = Deserialize(timeoutDataEntity.Headers)
            };
            return timeoutData;
        }

        /// <summary>
        /// Safe method for removing a timeout entry
        /// </summary>
        /// <param name="timeoutId">ID of the timeout you want to try deleting</param>
        /// <returns>True/False indicating successful or unsucessful deletion</returns>
        public bool TryRemove(string timeoutId)
        {
            try
            {
                TimeoutData data;
                return TryRemove(timeoutId, out data);
            }
            catch (DataServiceRequestException) // table entries were already removed
            {
                return false;
            }
            catch (StorageException) // blob file was already removed
            {
                return false;
            }
        }

        /// <summary>
        /// Safe method for removing a timeout entry
        /// </summary>
        /// <param name="timeoutId">ID of the timeout you want to try deleting</param>
        /// <param name="timeoutData">returns the object that deletion was attempted on</param>
        /// <returns>True/False indicating successful or unsucessful deletion</returns>
        public bool TryRemove(string timeoutId, out TimeoutData timeoutData)
        {
            timeoutData = null;
            
            var timeoutDataTable = client.GetTableReference(timeoutDataTableName);

            TimeoutDataEntity timeoutDataEntity;
            if (!TryGetTimeoutData(timeoutDataTable, timeoutId, string.Empty, out timeoutDataEntity))
            {
                return false;
            }

            var deleteOperation = TableOperation.Delete(timeoutDataEntity);
            timeoutDataTable.Execute(deleteOperation);

            timeoutData = new TimeoutData
            {
                Destination = Address.Parse(timeoutDataEntity.Destination),
                SagaId = timeoutDataEntity.SagaId,
                State = Download(timeoutDataEntity.StateAddress),
                Time = timeoutDataEntity.Time,
                Id = timeoutDataEntity.RowKey,
                OwningTimeoutManager = timeoutDataEntity.OwningTimeoutManager,
                Headers = Deserialize(timeoutDataEntity.Headers)
            };

            TimeoutDataEntity timeoutDataEntityBySaga;
            if (TryGetTimeoutData(timeoutDataTable, timeoutDataEntity.SagaId.ToString(), timeoutId, out timeoutDataEntityBySaga))
            {
                var deleteSagaOperation = TableOperation.Delete(timeoutDataEntityBySaga);
                timeoutDataTable.Execute(deleteSagaOperation);
            }

            TimeoutDataEntity timeoutDataEntityByTime;
            if (TryGetTimeoutData(timeoutDataTable, timeoutDataEntity.Time.ToString(partitionKeyScope), timeoutId, out timeoutDataEntityByTime))
            {
                var deleteByTimeOperation = TableOperation.Delete(timeoutDataEntityByTime);
                timeoutDataTable.Execute(deleteByTimeOperation);
            }

            RemoveState(timeoutDataEntity.StateAddress);

            return true;
        }

        /// <summary>
        /// Remove a single timeout entry
        /// </summary>
        /// <param name="sagaId">The saga ID used to find the timeout that will be removed</param>
        public void RemoveTimeoutBy(Guid sagaId)
        {
            var timeoutDataTable = client.GetTableReference(timeoutDataTableName);

            var query = (from c in timeoutDataTable.CreateQuery<TimeoutDataEntity>()
                where c.PartitionKey == sagaId.ToString()
                select c);

            foreach (var timeoutDataEntityBySaga in query.Take(1000))
            {
                RemoveState(timeoutDataEntityBySaga.StateAddress);

                TimeoutDataEntity timeoutDataEntityByTime;
                if (TryGetTimeoutData(timeoutDataTable, timeoutDataEntityBySaga.Time.ToString(partitionKeyScope), timeoutDataEntityBySaga.RowKey, out timeoutDataEntityByTime))
                {
                    var deleteOperation = TableOperation.Delete(timeoutDataEntityByTime);
                    timeoutDataTable.Execute(deleteOperation);
                }

                TimeoutDataEntity timeoutDataEntity;
                if (TryGetTimeoutData(timeoutDataTable, timeoutDataEntityBySaga.RowKey, string.Empty, out timeoutDataEntity))
                {
                    var deleteOperation = TableOperation.Delete(timeoutDataEntity);
                    timeoutDataTable.Execute(deleteOperation);
                }

                var sagaDeleteOperation = TableOperation.Delete(timeoutDataEntityBySaga);
                timeoutDataTable.Execute(sagaDeleteOperation);
            }
        }
        
        bool TryGetTimeoutData(CloudTable timeoutDataTable, string partitionKey, string rowKey, out TimeoutDataEntity result)
        {
            result = (from c in timeoutDataTable.CreateQuery<TimeoutDataEntity>()
                      where c.PartitionKey == partitionKey && c.RowKey == rowKey // issue #191 cannot occur when both partitionkey and rowkey are specified
                      select c).ToList().SafeFirstOrDefault();

            return result != null;

        }

        /// <summary>
        /// Verify if the timeout data has a lease
        /// </summary>
        /// <param name="data">The timeout data to check</param>
        /// <returns>True if the timeout data has a lease associated with it</returns>
        public bool CanSend(TimeoutData data)
        {
            var timeoutDataTable = client.GetTableReference(timeoutDataTableName);

            TimeoutDataEntity timeoutDataEntity;
            if (!TryGetTimeoutData(timeoutDataTable, data.Id, string.Empty, out timeoutDataEntity)) return false;

            var container = cloudBlobclient.GetContainerReference(timeoutStateContainerName);

            var leaseBlob = container.GetBlockBlobReference(timeoutDataEntity.StateAddress);
            using (var lease = new AutoRenewLease(leaseBlob))
            {
                return lease.HasLease;
            }
        }

        void Upload(byte[] state, string stateAddress)
        {
            var container = cloudBlobclient.GetContainerReference(timeoutStateContainerName);
            var blob = container.GetBlockBlobReference(stateAddress);
            using (var stream = new MemoryStream(state))
            {
                blob.UploadFromStream(stream);
            }
        }

        byte[] Download(string stateAddress)
        {
            var container = cloudBlobclient.GetContainerReference(timeoutStateContainerName);

            var blob = container.GetBlockBlobReference(stateAddress);
            using (var stream = new MemoryStream())
            {
                blob.DownloadToStream(stream);
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

        void RemoveState(string stateAddress)
        {
            var container = cloudBlobclient.GetContainerReference(timeoutStateContainerName);
            var blob = container.GetBlockBlobReference(stateAddress);
            blob.DeleteIfExists();
        }

        string Sanitize(string s)
        {
            var rgx = new Regex(@"[^a-zA-Z0-9\-_]");
            var n = rgx.Replace(s, "");
            return n;
        }

        bool TryGetLastSuccessfulRead(CloudTable timeoutManagerDataTable, out TimeoutManagerDataEntity lastSuccessfulReadEntity)
        {

            var query = from m in timeoutManagerDataTable.CreateQuery<TimeoutManagerDataEntity>()
                        where m.PartitionKey == sanitizedEndpointInstanceName
                        select m;

            lastSuccessfulReadEntity = query.SafeFirstOrDefault();
            
            return lastSuccessfulReadEntity != null;
        }

        void UpdateSuccessfulRead(CloudTable table, TimeoutManagerDataEntity read)
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
                    table.Execute(addOperation);
                }
                else
                {
                    var updateOperation = TableOperation.Replace(read);
                    table.Execute(updateOperation);
                }
            }
            catch (DataServiceRequestException ex) // handle concurrency issues
            {
                var response = ex.Response.FirstOrDefault();
                //Concurrency Exception - PreCondition Failed or Entity Already Exists
                if (response != null && (response.StatusCode == 412 || response.StatusCode == 409))
                {
                    return; 
                    // I assume we can ignore this condition? 
                    // Time between read and update is very small, meaning that another instance has sent 
                    // the timeout messages that this node intended to send and if not we will resend 
                    // anything after the other node's last read value anyway on next request.
                }

                throw;
            }

        }
        
        CloudTableClient client;
        CloudBlobClient cloudBlobclient;
    }
}
