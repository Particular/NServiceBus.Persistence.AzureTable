namespace NServiceBus.Persistence.AzureTable.Tests
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    static class SubscriptionPersisterExtensions
    {
        public static Task<IEnumerable<Subscriber>> GetSubscribers(this AzureSubscriptionStorage persister, params MessageType[] messageHierarchy)
        {
            return persister.GetSubscriberAddressesForMessage(messageHierarchy, null);
        }
    }
}