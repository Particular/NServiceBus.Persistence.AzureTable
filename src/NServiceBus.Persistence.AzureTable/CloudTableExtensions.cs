namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Data.Tables;

    static class CloudTableExtensions
    {
        public static async Task<IList<T>> ExecuteQueryAsync<T>(this TableClient table, Expression<Func<T, bool>> query, int take = int.MaxValue, CancellationToken cancellationToken = default) where T : ITableEntity, new()
        {
            var items = new List<T>();
            table.Query<T>(query).ToList();

            TableContinuationToken token = null;

            do
            {
                var seg = await table.ExecuteQuerySegmentedAsync(
                        query: query,
                        token: token,
                        requestOptions: null,
                        operationContext: null,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
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
            }
            while (token != null && !cancellationToken.IsCancellationRequested && items.Count < take);

            return items;
        }
    }
}