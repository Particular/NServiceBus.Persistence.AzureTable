namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using AcceptanceTesting.Support;

    public static class Serializers
    {
        static Serializers()
        {
            Xml = new RunDescriptor("Xml");
            Xml.Settings.Set("Serializer", typeof(XmlSerializer).AssemblyQualifiedName);

            var json = new RunDescriptor("Json");
            json.Settings.Set("Serializer", typeof(JsonSerializer).AssemblyQualifiedName);
        }

        public static readonly RunDescriptor Xml;
    }
}