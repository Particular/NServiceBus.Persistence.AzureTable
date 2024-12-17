namespace NServiceBus.AcceptanceTests;

using System;

class TableNameProvider(Func<string> tableNameProvider)
{
    public Func<string> GetTableName { get; } = tableNameProvider;
}