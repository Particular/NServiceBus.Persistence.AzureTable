namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ScenarioDescriptors;
    using AcceptanceTesting.Support;

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

        public static void DefineTransport(this EndpointConfiguration builder, RunSettings settings, Type endpointBuilderType)
        {
            if (!settings.ContainsKey("Transport"))
            {
                settings = Transports.Default.Settings;
            }

            const string typeName = "ConfigureTransport";

            var transportType = GetTypePersistent(settings.Get<string>("Transport"));

            if (transportType == null)
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.FullName)
                    .ToArray();

                var msg = $"Requested Transport: `{settings.Get<string>("Transport")}` but got null. Loaded Assemblies: {String.Join(", ", assemblies)}";
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

            builder.UseTransport(transportType).ConnectionString(settings.Get<string>("Transport.ConnectionString"));
        }

        public static void DefineTransactions(this EndpointConfiguration config, RunSettings settings)
        {
            if (settings.ContainsKey("Transactions.Disable"))
            {
                config.UseTransport(GetTypePersistent(settings.Get<string>("Transport"))).Transactions(TransportTransactionMode.None);
            }
            if (settings.ContainsKey("Transactions.SuppressDistributedTransactions"))
            {
                config.UseTransport(GetTypePersistent(settings.Get<string>("Transport"))).Transactions(TransportTransactionMode.ReceiveOnly);
            }
        }

        public static void DefinePersistence(this EndpointConfiguration config, RunSettings settings)
        {
            if (!settings.ContainsKey("Persistence"))
            {
                settings = Persistence.Default.Settings;
            }

            var persistenceType = Type.GetType(settings.Get<string>("Persistence"));


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

        public static void DefineBuilder(this EndpointConfiguration config, RunSettings settings)
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

            var builderType = Type.GetType(settings.Get<string>("Builder"));


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

        static bool ContainsKey(this RunSettings settings, string key)
        {
            return settings.Any(setting => setting.Key == key);
        }

        public static T GetOrNull<T>(this RunSettings settings, string key) where T : class
        {
            T result;
            settings.TryGet(key, out result);

            return result;
        }
    }
}