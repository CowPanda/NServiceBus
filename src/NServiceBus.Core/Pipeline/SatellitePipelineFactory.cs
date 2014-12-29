﻿namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Faults;
    using NServiceBus.Logging;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Satellites;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;
    using TransactionSettings = NServiceBus.Unicast.Transport.TransactionSettings;

    class SatellitePipelineFactory : PipelineFactory
    {
        public virtual IEnumerable<TransportReceiver> BuildPipelines(IBuilder builder, ReadOnlySettings settings, IExecutor executor)
        {
            var satellitesList = builder.BuildAll<ISatellite>()
                .ToList()
                .Where(s => !s.Disabled)
                .ToList();

            for (var index = 0; index < satellitesList.Count; index++)
            {
                var satellite = satellitesList[index];
                Logger.DebugFormat("Creating {1}/{2} {0} satellite", satellite.GetType().AssemblyQualifiedName,
                    index + 1, satellitesList.Count);

                var transactionSettings = new TransactionSettings(settings);
                if (satellite.InputAddress != null)
                {
                    var pipelineExecutor = BuildPipelineExecutor(builder);

                    var pipeline = new SatelliteTransportReceiver(
                        satellite.GetType().AssemblyQualifiedName,
                        transactionSettings,
                        builder.Build<IDequeueMessages>(),
                        satellite.InputAddress.Queue,
                        false,
                        pipelineExecutor,
                        executor,
                        builder.Build<IManageMessageFailures>(),
                        settings,
                        builder.Build<Configure>(),
                        satellite)
                    {
                        Notifications = builder.Build<BusNotifications>()
                    };

                    var advancedSatellite = satellite as IAdvancedSatellite;

                    if (advancedSatellite != null)
                    {
                        var receiverCustomization = advancedSatellite.GetReceiverCustomization();
                        receiverCustomization(pipeline);
                    }
                    yield return pipeline;
                }
                else
                {
                    Logger.DebugFormat("Skipping satellite {0} because its input queue is not configured.", satellite.GetType().AssemblyQualifiedName);
                }
            }
        }

        static PipelineExecutor BuildPipelineExecutor(IBuilder builder)
        {
            var pipelineModifications = new PipelineModifications();
            var pipelineSettings = new PipelineSettings(pipelineModifications);

            pipelineSettings.Register(builder.Build<TransportReceiveBehaviorDefinition>().Registration);
            pipelineSettings.Register<FirstLevelRetriesBehavior.Registration>();
            pipelineSettings.Register<ExecuteSatelliteHandlerBehavior.Registration>();

            var pipelineExecutor = new PipelineExecutor(builder, builder.Build<BusNotifications>(), pipelineModifications);
            return pipelineExecutor;
        }

        static readonly ILog Logger = LogManager.GetLogger<SatellitePipelineFactory>();
    }
}