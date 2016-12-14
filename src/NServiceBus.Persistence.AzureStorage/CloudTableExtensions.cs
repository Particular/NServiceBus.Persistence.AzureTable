namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Table;

    static class CloudTableExtensions
    {
        public static async Task<IList<T>> ExecuteQueryAsync<T>(this CloudTable table, TableQuery<T> query, int take = Int32.MaxValue, CancellationToken ct = default(CancellationToken)) where T : ITableEntity, new()
        {
            var items = new List<T>();
            TableContinuationToken token = null;

            do
            {
                var seg = await table.ExecuteQuerySegmentedAsync(query, token, ct).ConfigureAwait(false);
                token = seg.ContinuationToken;

                if (items.Count + seg.Results.Count > take)
                {
                    var numberToTake = items.Count + seg.Results.Count - take;
                    items.AddRange(seg.Take(seg.Results.Count - numberToTake));
                }
                else
                {
                    items.AddRange(seg);
                }
            } while (token != null && !ct.IsCancellationRequested && items.Count < take);

            return items;
        }

        public static async Task<IList<IListBlobItem>> ListBlobAsync(this CloudBlobContainer blob, int take = Int32.MaxValue, CancellationToken ct = default(CancellationToken))
        {
            var items = new List<IListBlobItem>();
            BlobContinuationToken token = null;

            do
            {
                var seg = await blob.ListBlobsSegmentedAsync(token, ct).ConfigureAwait(false);
                token = seg.ContinuationToken;

                var segCount = seg.Results.Count();
                if (items.Count + segCount > take)
                {
                    var numberToTake = items.Count + segCount - take;
                    items.AddRange(seg.Results.Take(segCount - numberToTake));
                }
                else
                {
                    items.AddRange(seg.Results);
                }
            } while (token != null && !ct.IsCancellationRequested && items.Count < take);

            return items;
        }

        /// <summary>
        /// Safely deletes an entitym ignoring not found exception.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static async Task DeleteIgnoringNotFound(this CloudTable table, ITableEntity entity)
        {
            try
            {
                await table.ExecuteAsync(TableOperation.Delete(entity)).ConfigureAwait(false);
            }
            catch (StorageException ex)
            {
                // Horrible logic to check if item has already been deleted or not
                var webException = ex.InnerException as WebException;
                if (webException?.Response != null)
                {
                    var response = (HttpWebResponse) webException.Response;
                    if ((int) response.StatusCode != 404)
                    {
                        // Was not a previously deleted exception, throw again
                        throw;
                    }
                }
            }
        }
    }
}