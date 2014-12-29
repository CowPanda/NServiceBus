namespace NServiceBus.Features
{
    using Config;
    using NServiceBus.FirstLevelRetries;
    using NServiceBus.Settings;

    /// <summary>
    /// Used to configure Second Level Retries.
    /// </summary>
    public class FirstLevelRetries : Feature
    {
        internal FirstLevelRetries()
        {
            EnableByDefault();
            Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Send only endpoints can't use FLR since it only applies to messages being received");

            Prerequisite(context => context.Settings.Get<bool>("Transactions.Enabled"), "Send only endpoints can't use FLR since it requires the transport to be able to rollback");

            Prerequisite(context => GetMaxRetries(context.Settings) > 0, "FLR was disabled in config since it's set to 0");
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<FlrStatusStorage>(DependencyLifecycle.SingleInstance);
             
            context.Pipeline.Register<FirstLevelRetriesBehavior.Registration, FirstLevelRetriesBehavior>(
                builder => new FirstLevelRetriesBehavior(builder.Build<FlrStatusStorage>(),GetMaxRetries(context.Settings),builder.Build<BusNotifications>()));
        }
       

        int GetMaxRetries(ReadOnlySettings settings)
        {
            var retriesConfig = settings.GetConfigSection<TransportConfig>();

            if (retriesConfig == null)
                return 5;

            return retriesConfig.MaxRetries;

        }

    }
}