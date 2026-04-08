namespace NServiceBus.TransactionalSession
{
    using Features;
    using Microsoft.Extensions.DependencyInjection;

    sealed class AzureTableTransactionalSession : TransactionalSession
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            // can be a singleton
            _ = context.Services
                .AddSingleton<IOpenSessionOptionsCustomization, SetAsDispatchedHolderOpenSessionOptionCustomization>();

            var endpointName = context.Settings.EndpointName();
            _ = context.Services
                .AddSingleton<IOpenSessionOptionsCustomization>(new OutboxEndpointNameOpenSessionOptionCustomization(endpointName));

            context.Pipeline.Register(new AzureTableControlMessageBehavior(),
                "Propagates control message header values to TableEntityPartitionKeys and TableInformation when necessary.");

            base.Setup(context);
        }
    }
}