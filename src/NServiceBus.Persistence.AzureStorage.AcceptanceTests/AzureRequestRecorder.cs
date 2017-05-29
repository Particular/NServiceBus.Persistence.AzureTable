using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Microsoft.WindowsAzure.Storage;

public class AzureRequestRecorder : IDisposable
{
    List<string> Requests = new List<string>();
        
    public AzureRequestRecorder()
    {
        OperationContext.GlobalSendingRequest += OnSendingRequest;
    }
    void OnSendingRequest(object sender, RequestEventArgs e)
    {
        if (ShouldLog(e))
        {
            Requests.Add($"{e.Request.Method,-7} {e.Request.RequestUri.PathAndQuery}");
        }
    }

    static bool ShouldLog(RequestEventArgs e)
    {
        var isDelete = e.Request.Method == HttpMethod.Delete.Method;

        if (isDelete && e.Request.RequestUri.PathAndQuery.EndsWith("/messages"))
        {
            // clearing queue
            return false;
        }

        return true;
    }

    public void Dispose()
    {
        OperationContext.GlobalSendingRequest -= OnSendingRequest;
    }

    public void Print(TextWriter @out)
    {
        @out.WriteLine("Recorded calls to Azure Storage Services:");

        foreach (var request in Requests)
        {
            @out.WriteLine($"- {request}");
        }

        @out.WriteLine();
    }
}