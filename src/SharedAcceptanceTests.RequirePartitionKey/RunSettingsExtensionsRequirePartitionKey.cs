namespace NServiceBus.AcceptanceTests;

using System;
using NServiceBus.AcceptanceTesting.Support;

public static partial class RunSettingsExtensions
{
    public static void AllowTableCreation(this RunSettings runSettings) =>
        runSettings.Set(new AllowTableCreation());

    public static void DoNotRegisterDefaultPartitionKeyProvider(this RunSettings runSettings) =>
        runSettings.Set(new DoNotRegisterDefaultPartitionKeyProvider());

    public static void DoNotRegisterDefaultTableNameProvider(this RunSettings runSettings) =>
        runSettings.Set(new DoNotRegisterDefaultTableNameProvider());

    public static void RegisterTableNameProvider(this RunSettings runSettings, Func<string> tableNameProvider) =>
        runSettings.Set(new TableNameProvider(tableNameProvider));
}