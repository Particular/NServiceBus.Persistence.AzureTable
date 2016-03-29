namespace NServiceBus.AzureStoragePersistence.SagaDeduplicator.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NServiceBus.AzureStoragePersistence.SagaDeduplicator.Index;
    using NUnit.Framework;

    public class ProgramTests
    {
        readonly CloudTableClient cloudTableClient;
        readonly string testDataDirectory;
        CloudTable cloudTable;
        readonly string connectionString;

        public ProgramTests()
        {
            connectionString = Environment.GetEnvironmentVariable("AzureStorageQueueTransport.ConnectionString");
            var account = CloudStorageAccount.Parse(connectionString);
            cloudTableClient = account.CreateCloudTableClient();

            var codeBase = new Uri(GetType().Assembly.CodeBase);
            var currentDirectory = Path.GetDirectoryName(codeBase.LocalPath);
            testDataDirectory = Path.Combine(currentDirectory, "data");
        }

        [SetUp]
        public void SetUp()
        {
            cloudTable = cloudTableClient.GetTableReference("Deduplicator" + Guid.NewGuid().ToString().Replace("-", ""));
            cloudTable.CreateIfNotExists();

            if (Directory.Exists(testDataDirectory))
            {
                IO.DeleteDirectory(testDataDirectory);
            }
        }

        [TearDown]
        public void TearDown()
        {
            cloudTable.DeleteAsync();
        }

        [Test]
        public void When_duplicated_saga_exists_Should_download_them_to_directory()
        {
            var g1_1 = new Guid("A7ADAA05-0E07-4620-B503-74DD9082CAB5");
            var g1_2 = new Guid("2340BBA9-B817-414D-AFE9-DFB38097BFCA");
            var g2_1 = new Guid("30DAB168-EE7F-49B2-B3AD-B18279461801");
            var g2_2 = new Guid("2B631DDB-FF1F-4997-B371-1370C08A0EA7");
            var g2_3 = new Guid("BF069CDC-3B51-4FDC-915A-AF0C7FA4E076");

            const string name1_1 = "name_1";
            const string name1_2 = "name_2";
            const int correlatingId_1 = 1;
            const int correlatingId_2 = 2;

            cloudTable.Execute(TableOperation.Insert(CreateEntity(g1_1, correlatingId_1, name1_1)));
            cloudTable.Execute(TableOperation.Insert(CreateEntity(g1_2, correlatingId_1, name1_2)));

            cloudTable.Execute(TableOperation.Insert(CreateEntity(g2_1, correlatingId_2, "anything")));
            cloudTable.Execute(TableOperation.Insert(CreateEntity(g2_2, correlatingId_2, "something")));
            cloudTable.Execute(TableOperation.Insert(CreateEntity(g2_3, correlatingId_2, "test")));

            var options = new Dictionary<string, string>
            {
                {Program.Keys.Directory, testDataDirectory},
                {Program.Keys.Operation, "Download"},
                {Program.Keys.SagaProperty, "CorrelatingId"},
                {Program.Keys.SagaTypeName, cloudTable.Name},
                {Program.Keys.ConnectionString, connectionString},
            };

            Program.Main(BuildOptions(options));

            var files = new DirectoryInfo(testDataDirectory).GetFiles("*.*", SearchOption.AllDirectories);

            // assert files
            var file1 = files.Single(f => f.Name == g1_1.ToString());
            AssertFile(file1, name1_1, correlatingId_1);
            var file2 = files.Single(f => f.Name == g1_2.ToString());
            AssertFile(file2, name1_2, correlatingId_1);

            // modify and run upload
            const string newName = "the_only_one";

            Update(file1, jo =>
            {
                jo.Property(SagaJsonMapper.ChooseThisSaga).Value = new JValue(true);
                jo.Property("Name").Value = new JValue(newName);
            });

            options[Program.Keys.Operation] = "Upload";
            Program.Main(BuildOptions(options));
        }

        private static string[] BuildOptions(Dictionary<string, string> options)
        {
            return options.Select(kvp => string.Concat(kvp.Key, kvp.Value)).ToArray();
        }

        private static void Update(FileInfo fi, Action<JObject> update)
        {
            var jo = LoadFile(fi);
            update(jo);
            using (var stream = fi.Open(FileMode.Create, FileAccess.Write))
            {
                using (var sw = new StreamWriter(stream))
                {
                    using (var jsonTextWriter = new JsonTextWriter(sw) { Formatting = Formatting.Indented })
                    {
                        jo.WriteTo(jsonTextWriter);
                        jsonTextWriter.Flush();
                    }
                }
            }
        }

        private static void AssertFile(FileInfo f, string name, int correlatingId)
        {
            var jo = LoadFile(f);
            Assert.AreEqual(name, (string)((JValue)jo["Name"]).Value);
            Assert.AreEqual(correlatingId, (long)((JValue)jo["CorrelatingId"]).Value);
            Assert.AreEqual(false, (bool)((JValue)jo[SagaJsonMapper.ChooseThisSaga]).Value);
            Assert.IsNotNullOrEmpty((string)((JValue)jo[SagaJsonMapper.ETag]).Value);
        }

        private static JObject LoadFile(FileInfo f)
        {
            using (var stream = f.OpenRead())
            {
                using (var textReader = new StreamReader(stream))
                {
                    using (var reader = new JsonTextReader(textReader))
                    {
                        return JObject.Load(reader);
                    }
                }
            }
        }

        private static SagaState CreateEntity(Guid g, int correlatingId, string name)
        {
            return new SagaState
            {
                PartitionKey = g.ToString(),
                RowKey = g.ToString(),
                CorrelatingId = correlatingId,
                Name = name
            };
        }

        private class SagaState : TableEntity
        {
            public long CorrelatingId { get; set; }
            public string Name { get; set; }
        }

    }
}