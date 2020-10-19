#if NETFRAMEWORK
namespace NServiceBus.Persistence.AzureStorage.ComponentTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Azure.Cosmos.Table;

    public class AzureRequestRecorder : IDisposable
    {
        public List<string> Requests = new List<string>();

        public AzureRequestRecorder()
        {
            OperationContext.GlobalSendingRequest += OnSendingRequest;
        }

        void OnSendingRequest(object sender, RequestEventArgs e)
        {
            Requests.Add($"{e.Request.Method,-7} {e.Request.RequestUri.PathAndQuery}");
        }

        public void Dispose()
        {
            OperationContext.GlobalSendingRequest -= OnSendingRequest;
        }

        public void Print(TextWriter @out)
        {
            @out.WriteLine("Recorded calls to Azure Storage Services");

            foreach (var request in Requests)
            {
                @out.WriteLine($"- {request}");
            }

            @out.WriteLine();
        }
    }
}
#endif