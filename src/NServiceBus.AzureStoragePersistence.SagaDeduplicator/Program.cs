namespace NServiceBus.AzureStoragePersistence.SagaDeduplicator
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Net;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;
    using NServiceBus.AzureStoragePersistence.SagaDeduplicator.Index;
    using NServiceBus.Hosting.Helpers;
    using NServiceBus.Saga;

    public class Program
    {
        public const string ConnectionStringName = "sagas";

        readonly string connectionString;
        private readonly string directory;

        private Program(string connectionString, string directory)
        {
            this.connectionString = connectionString;
            this.directory = directory;
        }

        private string GetSagaDirectory(string sagaTypeName)
        {
            return Path.Combine(directory, sagaTypeName);
        }

        private CloudTable GetTable(string sagaTypeName)
        {
            return SagaIndexer.GetTable(connectionString, sagaTypeName);
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

            var sagaTypes = FindAllSagaTypes();
            if (sagaTypes.Length == 0)
            {
                Console.WriteLine("No Saga types found! Have you put all the dlls with sagas in the deduplicator directory?");
                return;
            }

            var sagaToProperty = new Dictionary<string, string>();
            var sagasWithoutUnique = new List<string>();
            foreach (var sagaType in sagaTypes)
            {
                var uniqueProperty = UniqueAttribute.GetUniqueProperty(sagaType);
                if (uniqueProperty == null)
                {
                    sagasWithoutUnique.Add(sagaType.Name);
                }
                else
                {
                    sagaToProperty.Add(sagaType.Name, uniqueProperty.Name);
                }
            }

            PrintReport(sagaToProperty, sagasWithoutUnique);

            var settings = ConfigurationManager.ConnectionStrings[ConnectionStringName];

            string connectionString;
            if (!string.IsNullOrWhiteSpace(settings?.ConnectionString))
            {
                connectionString = settings.ConnectionString;
            }
            else
            {
                // try to parse the last param
                CloudStorageAccount account;
                var possibleConnectionString = args.Last();
                if (CloudStorageAccount.TryParse(possibleConnectionString, out account) == false)
                {
                    Console.WriteLine("Provide one connection string in the standard 'connectionStrings' App.config section with following name: '{0}'", ConnectionStringName);
                    return;
                }

                connectionString = possibleConnectionString;
            }

            var operation = (OperationType) Enum.Parse(typeof(OperationType), op, true);

            var program = new Program(connectionString, directory);
            switch (operation)
            {
                case OperationType.Download:
                    foreach (var kvp in sagaToProperty)
                    {
                        program.DownloadConflictingSagas(kvp.Key, kvp.Value);
                    }
                    return;
                case OperationType.Upload:
                    foreach (var kvp in sagaToProperty)
                    {
                        program.UploadResolvedConflicts(kvp.Key);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void PrintReport(Dictionary<string, string> sagaToProperty, List<string> sagasWithoutUnique)
        {
            Console.WriteLine();
            Console.WriteLine("Following saga types have correlation properties");
            foreach (var kvp in sagaToProperty)
            {
                Console.WriteLine($"\t* {kvp.Key} is correlated by: {kvp.Value}");
            }

            Console.WriteLine();
            Console.WriteLine("Following saga types have NO correlation property marked with [Unique] and won't be searched for duplicates.");
            foreach (var saga in sagasWithoutUnique)
            {
                Console.WriteLine($"\t* {saga}");
            }
        }

        private static Type[] FindAllSagaTypes()
        {
            var scanner = new AssemblyScanner
            {
                ThrowExceptions = false
            };
            return scanner.GetScannableAssemblies()
                .Assemblies.Concat(AppDomain.CurrentDomain.GetAssemblies())
                .Where(asm => asm.IsDynamic == false)
                .Distinct()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(IContainSagaData).IsAssignableFrom(t))
                .Where(t => t != typeof(IContainSagaData) && t != typeof(ContainSagaData))
                .ToArray();
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

        private void DownloadConflictingSagas(string sagaTypeName, string propertyName)
        {
            var sagaDirectory = GetSagaDirectory(sagaTypeName);
            Console.WriteLine();
            if (EnsureEmptyDirectoryExists(sagaDirectory) == false)
            {
                Console.WriteLine("Directory '{0}' must be empty", sagaTypeName);
                return;
            }

            var cloudTable = GetTable(sagaTypeName);
            if (cloudTable.Exists() == false)
            {
                Console.WriteLine("Table for saga '{0}' does not exist", sagaDirectory);
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

        private void UploadResolvedConflicts(string sagaTypeName)
        {
            var sagaDirectory = GetSagaDirectory(sagaTypeName);
            var cloudTable = GetTable(sagaTypeName);
            if (cloudTable.Exists() == false)
            {
                return;
            }

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
        }
    }
}