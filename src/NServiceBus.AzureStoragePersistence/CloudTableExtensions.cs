namespace NServiceBus
{
    using Microsoft.WindowsAzure.Storage.Table;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public static class CloudTableExtensions
    {
        public static async Task<IList<T>> ExecuteQueryAsync<T>(this CloudTable table, TableQuery<T> query, CancellationToken ct = default(CancellationToken)) where T : ITableEntity, new()
        {
            var items = new List<T>();
            var token = (TableContinuationToken)null;

            do
            {
                var seg = await table.ExecuteQuerySegmentedAsync(query, token, ct).ConfigureAwait(false);
                token = seg.ContinuationToken;
                items.AddRange(seg);

            } while (token != null && !ct.IsCancellationRequested);

            return items;
        }
    }
}