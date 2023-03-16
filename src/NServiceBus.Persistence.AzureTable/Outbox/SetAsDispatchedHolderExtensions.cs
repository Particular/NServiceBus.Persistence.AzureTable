#nullable enable

namespace NServiceBus.Persistence.AzureTable
{
    using System;

    static class SetAsDispatchedHolderExtensions
    {
        public static void ThrowIfTableClientIsNotSet(this SetAsDispatchedHolder setAsDispatchedHolder)
        {
            if (setAsDispatchedHolder.TableClientHolder != null)
            {
                return;
            }

            throw new Exception($"For the outbox to work a table name must be configured. Either configure a default one using '{nameof(ConfigureAzureStorage.DefaultTable)}' or set one via a behavior calling `context.Extensions.Set(new {nameof(TableInformation)}(\"SomeTableName\"))`");
        }
    }
}