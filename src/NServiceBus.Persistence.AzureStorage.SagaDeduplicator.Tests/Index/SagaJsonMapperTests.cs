using System;
using System.IO;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public class SagaJsonMapperTests
{
    CloudTable cloudTable;
    SagaJsonMapper mapper;

    public SagaJsonMapperTests()
    {
        var connectionString = Environment.GetEnvironmentVariable("AzureStorageQueueTransport.ConnectionString");
        var account = CloudStorageAccount.Parse(connectionString);
        var cloudTableClient = account.CreateCloudTableClient();
        var tableName = typeof(SagaDownloaderTestState).Name;
        cloudTable = cloudTableClient.GetTableReference(tableName);
        cloudTable.CreateIfNotExists();
        mapper = new SagaJsonMapper(connectionString, tableName);
    }

    [Test]
    public void When_download_of_existing_saga_requested_Should_download_saga()
    {
        var id = new Guid("8D65664D-A329-46B6-A11E-4E7A613261FC");
        const string name = "test";
        const int value = 1;

        EnsureEntity(id, name, value);

        using (var sw = new StringWriter())
        {
            mapper.Download(id, sw);

            var jo = JObject.Parse(sw.ToString());

            Assert.IsNotNullOrEmpty(jo.GetValue(SagaJsonMapper.ETag).ToString());

            Assert.AreEqual(name, (string)jo.GetValue("Name"));
            Assert.AreEqual(value, (int)jo.GetValue("Value"));
            Assert.IsNotNullOrEmpty((string)jo.GetValue(SagaJsonMapper.ETag));
            Assert.IsFalse((bool)jo.GetValue(SagaJsonMapper.ChooseThisSaga));
        }
    }

    [Test]
    public void When_uploaded_with_updated_props_and_matching_etag_Should_upload_saga()
    {
        var id = new Guid("8D65664D-A329-46B6-A11E-4E7A613261FC");
        const string name = "test";
        const string newName = "test_2";
        const int value = 1;
        const int newValue = 2;

        EnsureEntity(id, name, value);

        var jo = LoadSaga(id);

        jo.Property(SagaDownloaderTestState.Properties.Name).Value = new JValue(newName);
        jo.Property(SagaDownloaderTestState.Properties.Value).Value = new JValue(newValue);
        jo[SagaJsonMapper.ChooseThisSaga] = new JValue(true);
        jo["NewProperty"] = new JValue("NewPropertyValue");

        var status = mapper.TryUpload(id, new JTokenReader(jo));

        var saga = Download(id);
        Assert.AreEqual(SagaJsonMapper.UploadStatus.Selected, status);
        Assert.AreEqual(newName, saga.Name);
        Assert.AreEqual(newValue, saga.Value);
    }

    [Test]
    public void When_uploaded_with_not_matching_etag_Should_fail()
    {
        var id = new Guid("6F97FF8D-559F-4160-B713-C0C8F800AC99");
        const string name = "test";
        const int value = 1;

        EnsureEntity(id, name, value);

        var jo = LoadSaga(id);
        jo.Property(SagaJsonMapper.ETag).Value = new JValue("Invalid_ETag");
        jo[SagaJsonMapper.ChooseThisSaga] = new JValue(true);

        var status = mapper.TryUpload(id, new JTokenReader(jo));

        Assert.AreEqual(SagaJsonMapper.UploadStatus.DifferentETag, status);
    }

    [Test]
    public void When_uploaded_with_not_existing_saga_Should_fail()
    {
        var id = new Guid("D6F9F26D-91C6-4C47-9767-CE616C9F4963");
        var notExisting = new Guid("1BDA0406-D3DF-4439-B62E-9ADC2F829B0D");
        const string name = "test";
        const int value = 1;

        EnsureEntity(id, name, value);

        var jo = LoadSaga(id);
        jo.Property(SagaJsonMapper.ETag).Value = new JValue("Invalid_ETag");

        var status = mapper.TryUpload(notExisting, new JTokenReader(jo));

        Assert.AreEqual(SagaJsonMapper.UploadStatus.NotFound, status);
    }

    SagaDownloaderTestState Download(Guid id)
    {
        return (SagaDownloaderTestState) cloudTable.Execute(TableOperation.Retrieve<SagaDownloaderTestState>(id.ToString(), id.ToString())).Result;
    }

    JObject LoadSaga(Guid id)
    {
        using (var sw = new StringWriter())
        {
            mapper.Download(id, sw);
            return JObject.Parse(sw.ToString());
        }
    }

    void EnsureEntity(Guid existingSagaId, string name, int value)
    {
        var upsert = TableOperation.InsertOrReplace(new SagaDownloaderTestState
        {
            PartitionKey = existingSagaId.ToString(),
            RowKey = existingSagaId.ToString(),
            Name = name,
            Value = value,
        });

        cloudTable.Execute(upsert);
    }

    class SagaDownloaderTestState : TableEntity
    {
        public static class Properties
        {
            public const string Name = "Name";
            public const string Value = "Value";
        }

        public string Name { get; set; }
        public int Value { get; set; }
    }
}