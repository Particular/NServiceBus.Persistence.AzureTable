namespace NServiceBus.Persistence.AzureStorage
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class CurrentSharedTransactionalBatchBehavior : IBehavior<IIncomingLogicalMessageContext, IIncomingLogicalMessageContext>
    {
        public CurrentSharedTransactionalBatchBehavior(CurrentSharedTransactionalBatchHolder currentTransactionalBatchHolder)
        {
            this.currentTransactionalBatchHolder = currentTransactionalBatchHolder;
        }

        public async Task Invoke(IIncomingLogicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> next)
        {
            using (currentTransactionalBatchHolder.CreateScope())
            {
                await next(context).ConfigureAwait(false);
            }
        }

        readonly CurrentSharedTransactionalBatchHolder currentTransactionalBatchHolder;

    }
}