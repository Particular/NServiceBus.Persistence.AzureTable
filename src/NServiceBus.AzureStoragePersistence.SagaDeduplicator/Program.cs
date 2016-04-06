namespace NServiceBus.AzureStoragePersistence.SagaDeduplicator
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Net;
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;
    using NServiceBus.AzureStoragePersistence.SagaDeduplicator.Index;

    public class Program
    {
        public const string ConnectionStringName = "sagas";
        readonly CloudTable cloudTable;

        readonly string connectionString;
        readonly string propertyName;
        readonly string sagaDirectory;
        readonly string sagaTypeName;

        private Program(string connectionString, string sagaTypeName, string propertyName, string directory)
        {
            this.connectionString = connectionString;
            this.sagaTypeName = sagaTypeName;
            this.propertyName = propertyName;
            sagaDirectory = Path.Combine(directory, this.sagaTypeName);
            cloudTable = SagaIndexer.GetTable(connectionString, sagaTypeName);
        }

        public static void Main(string[] args)
        {
            string directory;
            string sagaTypeName;
            string propertyName;
            string op;

            if (TryFetchWithInfo(args, Keys.Directory, out directory) == false ||
                TryFetchWithInfo(args, Keys.SagaTypeName, out sagaTypeName) == false ||
                TryFetchWithInfo(args, Keys.SagaProperty, out propertyName) == false ||
                TryFetchWithInfo(args, Keys.Operation, out op) == false)
            {
                return;
            }

            string connectionString;
            if (TryFetch(args, Keys.ConnectionString, out connectionString) == false)
            {
                connectionString = ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString;
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    Console.WriteLine("Provide one connection string in the standard 'connectionStrings' App.config section with following name: '{0}'", ConnectionStringName);
                    return;
                }
            }

            var operation = (OperationType) Enum.Parse(typeof(OperationType), op, true);

            var program = new Program(connectionString, sagaTypeName, propertyName, directory);
            switch (operation)
            {
                case OperationType.Download:
                    program.DownloadConflictingSagas();
                    return;
                case OperationType.Upload:
                    program.UploadResolvedConflicts();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static bool TryFetchWithInfo(string[] args, string name, out string value)
        {
            if (TryFetch(args, name, out value))
            {
                return true;
            }

            Console.WriteLine("Parameter not set {0}", name);
            return false;
        }

        private static bool TryFetch(string[] args, string name, out string value)
        {
            foreach (var arg in args)
            {
                if (arg.StartsWith(name))
                {
                    value = arg.Substring(name.Length);
                    return true;
                }
            }

            value = null;
            return false;
        }

        private void DownloadConflictingSagas()
        {
            Console.WriteLine();
            if (EnsureEmptyDirectoryExists(sagaDirectory) == false)
            {
                Console.WriteLine("Directory '{0}' must be empty", sagaDirectory);
                return;
            }

            var mapper = new SagaJsonMapper(connectionString, sagaTypeName);

            var sagaIndexer = SagaIndexer.Get(connectionString, sagaTypeName, propertyName);

            Console.WriteLine($"Dowloading duplicates of saga '{sagaTypeName}' to '{sagaDirectory}'.");
            sagaIndexer.SearchForDuplicates((collidingPropertyValueHash, collidingSagas) =>
            {
                var alreadyDownloaded = new HashSet<Guid>();
                var workDir = Path.Combine(sagaDirectory, collidingPropertyValueHash.ToString());
                if (Directory.Exists(workDir) == false)
                {
                    Directory.CreateDirectory(workDir);
                }
                else
                {
                    var files = Directory.EnumerateFiles(workDir).Select(filePath => filePath.Substring(filePath.LastIndexOf("\\", StringComparison.Ordinal) + 1)).Select(Guid.Parse);

                    foreach (var sagaId in files)
                    {
                        alreadyDownloaded.Add(sagaId);
                    }
                }

                foreach (var sagaId in collidingSagas.ToArray().Except(alreadyDownloaded))
                {
                    using (var file = File.OpenWrite(Path.Combine(workDir, sagaId.ToString())))
                    {
                        using (var sw = new StreamWriter(file))
                        {
                            Console.WriteLine($"* Downloading saga {sagaId}");
                            mapper.Download(sagaId, sw);
                        }
                    }
                }
            });
        }

        private void UploadResolvedConflicts()
        {
            Console.WriteLine();
            var mapper = new SagaJsonMapper(connectionString, sagaTypeName);

            foreach (var duplicatedSagaToResolve in new DirectoryInfo(sagaDirectory).GetDirectories())
            {
                var sagaInstances = duplicatedSagaToResolve.GetFiles();

                Console.WriteLine();
                Console.WriteLine("Resolving conflict between following sagas:");

                var anySuccess = false;
                var toDelete = new List<Guid>();

                foreach (var instance in sagaInstances)
                {
                    using (var reader = new JsonTextReader(new StreamReader(instance.OpenRead())))
                    {
                        var id = Guid.Parse(instance.Name);

                        var status = mapper.TryUpload(id, reader);
                        if (status == SagaJsonMapper.UploadStatus.Selected)
                        {
                            anySuccess = true;
                        }
                        else
                        {
                            toDelete.Add(id);
                        }
                        Console.WriteLine(" * {0} {1}", instance.Name, status);
                    }
                }

                Console.WriteLine();
                if (anySuccess)
                {
                    Console.WriteLine(" Resolving successful. Deleting following duplicates:");
                    foreach (var id in toDelete)
                    {
                        var status = (HttpStatusCode) cloudTable.Execute(TableOperation.Delete(new TableEntity(id.ToString(), id.ToString())
                        {
                            ETag = "*"
                        })).HttpStatusCode;
                        if (status == HttpStatusCode.OK || status == HttpStatusCode.NoContent)
                        {
                            Console.WriteLine(" * {0}", id);
                        }
                    }
                }
                else
                {
                    Console.WriteLine(" No conflicting saga marked with $Choose_this_saga: true");
                }
            }
        }

        private static bool EnsureEmptyDirectoryExists(string directory)
        {
            if (Directory.Exists(directory) == false)
            {
                Directory.CreateDirectory(directory);
                return true;
            }

            return !Directory.EnumerateFileSystemEntries(directory).Any();
        }

        public class Keys
        {
            public const string Directory = "directory=";
            public const string SagaTypeName = "sagaTypeName=";
            public const string SagaProperty = "sagaProperty=";
            public const string Operation = "operation=";
            public const string ConnectionString = "connectionString=";
        }
    }
}