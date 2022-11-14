namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Azure.Core;
    using Azure.Core.Pipeline;

    /// <summary>
    /// This policy counts transaction batches by looking for the "boundary" header in the request
    /// </summary>
    sealed class TransactionalBatchCounterPolicy : HttpPipelinePolicy
    {
        public HashSet<string> BatchIdentifiers { get; } = new();

        public override void Process(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
        {
            CheckForBatch(message);
            ProcessNext(message, pipeline);
        }

        public override async ValueTask ProcessAsync(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
        {
            CheckForBatch(message);
            await ProcessNextAsync(message, pipeline);
        }

        void CheckForBatch(HttpMessage message)
        {
            if (message.Request.Headers.TryGetValue("Content-Type", out var contentTypeHeaderValue))
            {
                var contentTypeHeader = MediaTypeHeaderValue.Parse(contentTypeHeaderValue);
                var boundary = contentTypeHeader.Parameters.SingleOrDefault(x => x.Name == "boundary");
                if (boundary != null)
                {
                    BatchIdentifiers.Add(boundary.Value);
                }
            }
        }
    }
}