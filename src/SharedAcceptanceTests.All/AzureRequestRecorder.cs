namespace NServiceBus.AcceptanceTests;

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Core.Pipeline;

public class AzureRequestRecorder : HttpPipelinePolicy
{
    public ConcurrentQueue<string> Requests { get; } = new();

    public override void Process(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
    {
        CaptureRequest(message);
        ProcessNext(message, pipeline);
    }

    public override async ValueTask ProcessAsync(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
    {
        CaptureRequest(message);
        await ProcessNextAsync(message, pipeline);
    }

    void CaptureRequest(HttpMessage message)
        => Requests.Enqueue($"{message.Request.Method,-7} {message.Request.Uri.PathAndQuery}");

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