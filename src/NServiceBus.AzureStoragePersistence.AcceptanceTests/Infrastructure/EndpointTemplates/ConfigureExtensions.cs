namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ScenarioDescriptors;

    public static class ConfigureExtensions
    {
        public static string GetOrNull(this IDictionary<string, string> dictionary, string key)
        {
            if (!dictionary.ContainsKey(key))
            {
                return null;
            }

            return dictionary[key];
        }

        private static Type GetTypePersistent(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null)
                return type;

            int firstComma = typeName.IndexOf(',');
            string assemName = typeName.Substring(firstComma + 1).TrimStart();
            string className = typeName.Substring(0, firstComma);
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (string.Equals(a.FullName, assemName, StringComparison.InvariantCultureIgnoreCase))
                {
                    type = a.GetType(className);
                    if (type != null)
                        return type;
                }
            }

            return null;
        }

        public static void DefineTransport(this BusConfiguration builder, IDictionary<string, string> settings, Type endpointBuilderType)
        {
            if (!settings.ContainsKey("Transport"))
            {
                settings = Transports.Default.Settings;
            }

            const string typeName = "ConfigureTransport";

            var transportType = GetTypePersistent(settings["Transport"]);

            if (transportType == null)
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.FullName)
                    .ToArray();

                var msg = $"Requested Transport: `{settings["Transport"]}` but got null. Loaded Assemblies: {String.Join(", ", assemblies)}";
                throw new InvalidOperationException(msg);
            }

            var transportTypeName = "Configure" + transportType.Name;

            var configurerType = endpointBuilderType.GetNestedType(typeName) ??
                                 Type.GetType(transportTypeName, false);

            if (configurerType != null)
            {
                var configurer = Activator.CreateInstance(configurerType);

                dynamic dc = configurer;

                dc.Configure(builder);
                return;
            }

            builder.UseTransport(transportType).ConnectionString(settings["Transport.ConnectionString"]);
        }

        public static void DefineTransactions(this BusConfiguration config, IDictionary<string, string> settings)
        {
            if (settings.ContainsKey("Transactions.Disable"))
            {
                config.Transactions().Disable();
            }
            if (settings.ContainsKey("Transactions.SuppressDistributedTransactions"))
            {
                config.Transactions().DisableDistributedTransactions();
            }
        }

        public static void DefinePersistence(this BusConfiguration config, IDictionary<string, string> settings)
        {
            if (!settings.ContainsKey("Persistence"))
            {
                settings = Persistence.Default.Settings;
            }

            var persistenceType = Type.GetType(settings["Persistence"]);


            var typeName = "Configure" + persistenceType.Name;

            var configurerType = Type.GetType(typeName, false);

            if (configurerType != null)
            {
                var configurer = Activator.CreateInstance(configurerType);

                dynamic dc = configurer;

                dc.Configure(config);
                return;
            }

            config.UsePersistence(persistenceType);
        }

        public static void DefineBuilder(this BusConfiguration config, IDictionary<string, string> settings)
        {
            if (!settings.ContainsKey("Builder"))
            {
                var builderDescriptor = Builders.Default;

                if (builderDescriptor == null)
                {
                    return; //go with the default builder
                }

                settings = builderDescriptor.Settings;
            }

            var builderType = Type.GetType(settings["Builder"]);


            var typeName = "Configure" + builderType.Name;

            var configurerType = Type.GetType(typeName, false);

            if (configurerType != null)
            {
                var configurer = Activator.CreateInstance(configurerType);

                dynamic dc = configurer;

                dc.Configure(config);
            }

            config.UseContainer(builderType);
        }
    }
}