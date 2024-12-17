namespace NServiceBus.AcceptanceTests;

using System;

class TableNameProvider
{
    public TableNameProvider(Func<string> tableNameProvider)
    {
        GetTableName = tableNameProvider;
    }

    public Func<string> GetTableName { get; }
}