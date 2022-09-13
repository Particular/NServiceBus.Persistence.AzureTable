namespace NServiceBus.TransactionalSession
{
    using Features;

    sealed class AzureTableTransactionalSession : TransactionalSession
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            // can be a singleton
            context.Container.ConfigureComponent<SetAsDispatchedHolderOpenSessionOptionCustomization>(DependencyLifecycle.SingleInstance);
            context.Pipeline.Register(new AzureTableControlMessageBehavior(),
                "Propagates control message header values to TableEntityPartitionKeys and TableInformation when necessary.");

            base.Setup(context);
        }
    }
}