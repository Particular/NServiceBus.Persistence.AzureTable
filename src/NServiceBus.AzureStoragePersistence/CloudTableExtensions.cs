namespace NServiceBus
{
    using System;
    using Microsoft.WindowsAzure.Storage.Table;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public static class CloudTableExtensions
    {
        public static async Task<IList<T>> ExecuteQueryAsync<T>(this CloudTable table, TableQuery<T> query, int take = Int32.MaxValue, CancellationToken ct = default(CancellationToken)) where T : ITableEntity, new()
        {
            var items = new List<T>();
            TableContinuationToken token = null;

            do
            {
                var seg = await table.ExecuteQuerySegmentedAsync(query, token, ct).ConfigureAwait(false);
                token = seg.ContinuationToken;

                if (items.Count + seg.Count() > take)
                {
                    var numberToTake = items.Count + seg.Count() - take;
                    items.AddRange(seg.Take(seg.Count() - numberToTake));
                }
                else
                {
                    items.AddRange(seg);
                }

            } while (token != null && !ct.IsCancellationRequested && items.Count < take);

            return items;
        }
    }
}