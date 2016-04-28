using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

public sealed class SagaIndexer
{
    const int InitialBufferSize = 1024*1024;
    string[] columns;
    IEqualityComparer<object> equalityComparer;
    Func<object, ulong> hashingTransformer;
    string indexPropertyName;
    CloudTable table;

    public SagaIndexer(CloudTable table, string indexPropertyName, Func<object, ulong> hashingTransformer, IEqualityComparer<object> equalityComparer)
    {
        this.indexPropertyName = indexPropertyName;
        this.hashingTransformer = hashingTransformer;
        this.equalityComparer = equalityComparer;
        this.table = table;
        columns = new[]
        {
            "PartitionKey",
            "RowKey",
            this.indexPropertyName
        };
    }

    public void SearchForDuplicates(Action<Guid, IEnumerable<Guid>> onCollision)
    {
        var query = new TableQuery
        {
            SelectColumns = columns
        };

        TableContinuationToken token = null;

        var buffers = new List<IdHashBuffer>();
        var buffer = new IdHashBuffer(InitialBufferSize);
        buffers.Add(buffer);

        var collisionBytes = new byte[4096];
        var ms = new MemoryStream(collisionBytes);
        var sw = new StreamWriter(ms);

        do
        {
            var executeQuerySegmented = table.ExecuteQuerySegmented(query, token);
            foreach (var dte in executeQuerySegmented.Results)
            {
                var id = Guid.Parse(dte.PartitionKey);
                EntityProperty property;
                if (dte.Properties.TryGetValue(indexPropertyName, out property))
                {
                    var hash = hashingTransformer(property.PropertyAsObject);

                    if (buffer.TryWrite(id, hash) == false)
                    {
                        buffer = new IdHashBuffer(buffer.Size*2);
                        buffers.Add(buffer);

                        if (buffer.TryWrite(id, hash) == false)
                        {
                            throw new OutOfMemoryException();
                        }
                    }
                }
            }
            token = executeQuerySegmented.ContinuationToken;
        } while (token != null);

        foreach (var b in buffers)
        {
            b.Seal();
        }

        for (var i = 0; i < buffers.Count; i++)
        {
            var b = buffers[i];
            b.FindHashCollisions(buffers.Skip(i), (hash, ids) =>
            {
                var collisions = ids.Select(id => new TableQuery
                {
                    FilterString = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, id.ToString()),
                    SelectColumns = columns
                })
                    .Select(q => table.ExecuteQuery(q).SingleOrDefault())
                    .Where(dte => dte != null && dte.Properties.ContainsKey(indexPropertyName))
                    .GroupBy(dte => dte.Properties[indexPropertyName].PropertyAsObject, dte => Guid.Parse(dte.PartitionKey), equalityComparer)
                    .Where(g => g.Count() > 1)
                    .ToArray();

                foreach (var collision in collisions)
                {
                    JsonSerializer.Create().Serialize(sw, collision.Key);

                    sw.Flush();
                    var guid = new Guid(MD5.Create().ComputeHash(collisionBytes, 0, (int) ms.Position));
                    ms.Position = 0;

                    onCollision(guid, collision);
                }
            });
        }
    }

    public static SagaIndexer Get(string connectionString, string sagaTypeName, string indexPropertyName, Func<EdmType, Func<object, ulong>> hasherProvider = null)
    {
        var table = GetTable(connectionString, sagaTypeName);
        var indexPropertyType = table.GetPropertyType(indexPropertyName);

        hasherProvider = hasherProvider ?? HashingTransformers.GetHashingTransformer;

        return new SagaIndexer(table, indexPropertyName, hasherProvider(indexPropertyType), EqualityComparers.GetValueComparer(indexPropertyType));
    }

    internal static CloudTable GetTable(string connectionString, string sagaTypeName)
    {
        var account = CloudStorageAccount.Parse(connectionString);
        var client = account.CreateCloudTableClient();
        return client.GetTableReference(sagaTypeName);
    }
}