namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Data.Tables;

    static class TableClientExtensions
    {
        public static async Task ExecuteQueryAsync<T>(this TableClient table, Expression<Func<T, bool>> query,
                                                      CancellationToken cancellationToken = default) where T : class, ITableEntity, new()
        {
            var items = new List<T>();
            AsyncPageable<T> queryResults = table.QueryAsync(query, cancellationToken: cancellationToken);

            await foreach (Page<T> page in queryResults.AsPages().WithCancellation(cancellationToken))
            {
                items.AddRange(page.Values);
            }
        }

        public static async Task ExecuteQueryAsync<T>(this TableClient table, string query,
                                                      CancellationToken cancellationToken = default) where T : class, ITableEntity, new()
        {
            var items = new List<T>();
            AsyncPageable<T> queryResults = table.QueryAsync<T>(query, cancellationToken: cancellationToken);

            await foreach (Page<T> page in queryResults.AsPages().WithCancellation(cancellationToken))
            {
                items.AddRange(page.Values);
            }
        }
    }
}