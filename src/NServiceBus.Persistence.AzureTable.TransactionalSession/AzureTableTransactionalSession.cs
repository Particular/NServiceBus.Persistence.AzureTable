namespace NServiceBus.TransactionalSession
{
    using Features;
    using Microsoft.Extensions.DependencyInjection;
    using SynchronizedStorage = Persistence.AzureTable.SynchronizedStorage;

    sealed class AzureTableTransactionalSession : Feature
    {
        public AzureTableTransactionalSession()
        {
            EnableByDefault();
            DependsOn<SynchronizedStorage>();
            DependsOn<TransactionalSessionFeature>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            // can be a singleton
            context.Services
                .AddSingleton<IOpenSessionOptionsCustomization, SetAsDispatchedHolderOpenSessionOptionCustomization>();
            context.Pipeline.Register(new AzureTableControlMessageBehavior(),
                "Propagates control message header values to TableEntityPartitionKeys and TableInformation when necessary.");
        }
    }
}