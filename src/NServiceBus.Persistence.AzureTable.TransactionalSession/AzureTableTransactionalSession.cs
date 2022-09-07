namespace NServiceBus.TransactionalSession
{
    using Features;
    using Microsoft.Extensions.DependencyInjection;

    sealed class AzureTableTransactionalSession : TransactionalSession
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            // can be a singleton
            context.Services
                .AddSingleton<IOpenSessionOptionsCustomization, SetAsDispatchedHolderOpenSessionOptionCustomization>();
            context.Pipeline.Register(new AzureTableControlMessageBehavior(),
                "Propagates control message header values to TableEntityPartitionKeys and TableInformation when necessary.");

            base.Setup(context);
        }
    }
}