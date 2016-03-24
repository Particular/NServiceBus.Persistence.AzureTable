namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AcceptanceTesting.Support;
    using NServiceBus.Persistence;

    public static class Persistence
    {
        static Persistence()
        {
            AzureStoragePersistenceDescriptor = new RunDescriptor(AzureStoragePersistenceType.Name);
            AzureStoragePersistenceDescriptor.Settings.Set("Persistence", AzureStoragePersistenceType.AssemblyQualifiedName);
        }

        public static RunDescriptor Default
        {
            get
            {
                var specificPersistence = Environment.GetEnvironmentVariable("Persistence.UseSpecific");

                if (!string.IsNullOrEmpty(specificPersistence))
                {
                    return AllAvailable.Single(r => r.Key == specificPersistence);
                }

                var nonCorePersister = AllAvailable.FirstOrDefault();

                if (nonCorePersister != null)
                {
                    return nonCorePersister;
                }

                return AzureStoragePersistenceDescriptor;
            }
        }

        static IEnumerable<RunDescriptor> AllAvailable
        {
            get
            {
                if (availablePersisters == null)
                {
                    availablePersisters = GetAllAvailable().ToList();
                }

                return availablePersisters;
            }
        }

        static Type AzureStoragePersistenceType = typeof(AzureStoragePersistence);
        static RunDescriptor AzureStoragePersistenceDescriptor;

        static IEnumerable<RunDescriptor> GetAllAvailable()
        {
            var foundDefinitions = TypeScanner.GetAllTypesAssignableTo<PersistenceDefinition>()
                .Where(t => t.Assembly != AzureStoragePersistenceType.Assembly &&
                t.Assembly != typeof(Persistence).Assembly);

            foreach (var definition in foundDefinitions)
            {
                var key = definition.Name;

                var runDescriptor = new RunDescriptor(key);
                runDescriptor.Settings.Set("Persistence", definition.AssemblyQualifiedName);

                yield return runDescriptor;
            }
        }

        static IList<RunDescriptor> availablePersisters;
    }
}