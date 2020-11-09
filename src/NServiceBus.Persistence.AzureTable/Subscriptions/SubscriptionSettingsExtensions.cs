namespace NServiceBus.Persistence.AzureTable
{
    using Settings;

    static class SubscriptionSettingsExtensions
    {
        public static string GetSubscriptionTableName(this ReadOnlySettings readOnlySettings)
        {
            // the subscription storage specific override takes precedence for backward compatibility
            string subscriptionTableName;
            if (readOnlySettings.TryGet<TableInformation>(out var info) && !readOnlySettings.HasExplicitValue(WellKnownConfigurationKeys.SubscriptionStorageTableName))
            {
                subscriptionTableName = info.TableName;
            }
            else
            {
                subscriptionTableName = readOnlySettings.Get<string>(WellKnownConfigurationKeys.SubscriptionStorageTableName);
            }

            return subscriptionTableName;
        }
    }
}