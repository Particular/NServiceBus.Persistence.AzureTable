namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using System.Collections.Generic;
    using AcceptanceTesting.Support;

    public static class Serializers
    {
        static Serializers()
        {
            Xml = new RunDescriptor("Xml");
            Xml.Settings.Set("Serializer", typeof(XmlSerializer).AssemblyQualifiedName);

            Json = new RunDescriptor("Json");
            Json.Settings.Set("Serializer", typeof(JsonSerializer).AssemblyQualifiedName);
        }

        public static readonly RunDescriptor Xml;
        public static readonly RunDescriptor Json;
    }
}