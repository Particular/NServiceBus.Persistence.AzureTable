namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using AcceptanceTesting.Support;
    using Hosting.Helpers;
    using NServiceBus;
    using NServiceBus.Config.ConfigurationSource;
    using System.Threading.Tasks;
    using Features;
    using ObjectBuilder;

    public class DefaultServer : IEndpointSetupTemplate
    {
        List<Type> typesToInclude;

        public DefaultServer()
        {
            typesToInclude = new List<Type>();
        }

        public DefaultServer(List<Type> typesToInclude)
        {
            this.typesToInclude = typesToInclude;
        }

        public Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, IConfigurationSource configSource, Action<EndpointConfiguration> configurationBuilderCustomization)
        {
            var types = GetTypesScopedByTestClass(endpointConfiguration);

            typesToInclude.AddRange(types);

            var builder = new EndpointConfiguration();

            builder.EndpointName(endpointConfiguration.EndpointName);
            builder.CustomConfigurationSource(configSource);
            builder.EnableInstallers();

            builder.DisableFeature<TimeoutManager>();
            builder.DisableFeature<SecondLevelRetries>();
            builder.DisableFeature<FirstLevelRetries>();

            builder.RegisterComponents(r => { RegisterInheritanceHierarchyOfContextOnContainer(runDescriptor, r); });

            var connectionString = Environment.GetEnvironmentVariable("AzureStoragePersistence.ConnectionString");
            builder.UsePersistence<AzureStoragePersistence>().ConnectionString(connectionString);

            // Use this to disable synchronization storage by providing an empty implementation
            // so that when the container tries to resolve an instance if ISynchronizedStorage it
            // doesn't throw a ComponentNotRegisteredException exception
            // This is hacky and we need a proper way around this.
            builder.EnableFeature<FakeSynchronizationStorage>();

            configurationBuilderCustomization(builder);

            return Task.FromResult(builder);
        }

        static void RegisterInheritanceHierarchyOfContextOnContainer(RunDescriptor runDescriptor, IConfigureComponents r)
        {
            var type = runDescriptor.ScenarioContext.GetType();
            while (type != typeof(object))
            {
                r.RegisterSingleton(type, runDescriptor.ScenarioContext);
                type = type.BaseType;
            }
        }

        static IEnumerable<Type> GetTypesScopedByTestClass(EndpointCustomizationConfiguration endpointConfiguration)
        {
            var assemblies = new AssemblyScanner().GetScannableAssemblies();

            var types = assemblies.Assemblies
                                  //exclude all test types by default
                                  .Where(a =>
                                  {
                                      var references = a.GetReferencedAssemblies();

                                      return references.All(an => an.Name != "nunit.framework");
                                  })
                                  .SelectMany(a => a.GetTypes());


            types = types.Union(GetNestedTypeRecursive(endpointConfiguration.BuilderType.DeclaringType, endpointConfiguration.BuilderType));

            types = types.Union(endpointConfiguration.TypesToInclude);

            return types.Where(t => !endpointConfiguration.TypesToExclude.Contains(t)).ToList();
        }

        static IEnumerable<Type> GetNestedTypeRecursive(Type rootType, Type builderType)
        {
            if (rootType == null)
            {
                throw new InvalidOperationException("Make sure you nest the endpoint infrastructure inside the TestFixture as nested classes");
            }

            yield return rootType;

            if (typeof(IEndpointConfigurationFactory).IsAssignableFrom(rootType) && rootType != builderType)
                yield break;

            foreach (var nestedType in rootType.GetNestedTypes(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).SelectMany(t => GetNestedTypeRecursive(t, builderType)))
            {
                yield return nestedType;
            }
        }
    }
}