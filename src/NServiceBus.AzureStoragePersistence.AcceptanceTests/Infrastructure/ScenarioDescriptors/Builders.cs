namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using System.Collections.Generic;
    using System.Linq;
    using AcceptanceTesting.Support;
    using Container;

    public static class Builders
    {
        static IEnumerable<RunDescriptor> GetAllAvailable()
        {
            var builders = TypeScanner.GetAllTypesAssignableTo<ContainerDefinition>()
                .Where(t => !t.Assembly.FullName.StartsWith("NServiceBus.Core"))//exclude the default builder
                .ToList();

            return builders.Select(builder =>
            {
                var runDescriptor = new RunDescriptor(builder.Name);
                runDescriptor.Settings.Set("Builder", builder.AssemblyQualifiedName);

                return runDescriptor;
            });
        }

        public static RunDescriptor Default
        {
            get
            {
                return GetAllAvailable().FirstOrDefault();
            }
        }
    }
}