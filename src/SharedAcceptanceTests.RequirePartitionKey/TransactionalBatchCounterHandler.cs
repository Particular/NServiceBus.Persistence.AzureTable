using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http.Headers;
using Microsoft.Azure.Cosmos.Table;

sealed class TransactionalBatchCounterHandler : IDisposable
{
    public static int TotalTransactionalBatches => batchIdentifiers.Keys.Count;

    static ConcurrentDictionary<string, string> batchIdentifiers = new ConcurrentDictionary<string, string>();

    public TransactionalBatchCounterHandler()
    {
        OperationContext.GlobalSendingRequest += OnSendingRequest;
    }

    private void OnSendingRequest(object sender, RequestEventArgs e)
    {
        var contentTypeHeader = e.Request?.Content?.Headers?.ContentType;

        if (contentTypeHeader == null || contentTypeHeader.Parameters.All(x => x.Name != "boundary"))
        {
            return;
        }

        var boundary = contentTypeHeader.Parameters.Single(x => x.Name == "boundary");
        if (boundary != null)
        {
            batchIdentifiers.GetOrAdd(boundary.Value, boundary.Value);
        }
    }

    public static void Reset()
    {
        batchIdentifiers.Clear();
    }

    public void Dispose()
    {
        OperationContext.GlobalSendingRequest -= OnSendingRequest;
        Reset();
    }
}