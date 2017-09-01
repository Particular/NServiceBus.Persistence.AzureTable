namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Subscriptions
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

        /// <summary>Workaround for issue https://github.com/approvals/ApprovalTests.Net/issues/61</summary>
        public static void AwaitFix(this Task task)
        {
            task.GetAwaiter().GetResult();
        }

        /// <summary>Workaround for issue https://github.com/approvals/ApprovalTests.Net/issues/61</summary>
        public static T AwaitFix<T>(this Task<T> task)
        {
            return task.GetAwaiter().GetResult();
        }
    }
}