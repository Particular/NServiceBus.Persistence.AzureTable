using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.AcceptanceTests.ScenarioDescriptors;
using System;
using System.Collections.Generic;

public class ConfigureScenariosForMsmqTransport : IConfigureSupportedScenariosForTestExecution
{
    public IEnumerable<Type> UnsupportedScenarioDescriptorTypes { get; } = new[] { typeof(AllTransportsWithCentralizedPubSubSupport) };
}