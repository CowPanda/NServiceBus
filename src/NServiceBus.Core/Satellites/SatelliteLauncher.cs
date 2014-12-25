namespace NServiceBus.Satellites
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Config;
    using Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;
    using ObjectBuilder;
    using Unicast.Transport;

    class SatelliteLauncher
    {
        IBuilder builder;

        public SatelliteLauncher(IBuilder builder)
        {
            this.builder = builder;
        }

        public void Start()
        {
            var satellitesList = builder.BuildAll<ISatellite>()
                                        .ToList()
                                        .Where(s => !s.Disabled)
                                        .ToList();


            var satelliteContexts = new SatelliteContext[satellitesList.Count];

            Parallel.For(0, satellitesList.Count, index =>
                {
                    var satellite = satellitesList[index];

                    Logger.DebugFormat("Starting {1}/{2} {0} satellite", satellite.GetType().AssemblyQualifiedName,
                                       index + 1, satellitesList.Count);

                    var satelliteContext = new SatelliteContext
                        {
                            Instance = satellite
                        };

                    if (satellite.InputAddress != null)
                    {
                        satelliteContext.Transport = builder.Build<SatelliteTransportReceiver>();

                        var advancedSatellite = satellite as IAdvancedSatellite;
                        
                        if (advancedSatellite != null)
                        {
                            var receiverCustomization = advancedSatellite.GetReceiverCustomization();

                            receiverCustomization(satelliteContext.Transport);
                        }

                        var pipelineModifications = new PipelineModifications();

                        var transportBehaviorDefinition = builder.Build<TransportReceiveBehaviorDefinition>();
                        var pipelineSettings = new PipelineSettings(pipelineModifications);


                        pipelineSettings.Register(transportBehaviorDefinition.Registration);
                        
                        //todo: add FLR after refactoring it to be multi pipeline safe
                        pipelineSettings.Register<ExecuteSatelliteHandlerBehavior.Registration>();
                        

                        satelliteContext.PipelineExecutor = new PipelineExecutor(builder, builder.Build<BusNotifications>(),pipelineModifications);

                        satelliteContext.Transport.SetContext(satelliteContext);
                    }

                    StartSatellite(satelliteContext);

                    satelliteContexts[index] = satelliteContext;

                    Logger.InfoFormat("Started {1}/{2} {0} satellite", satellite.GetType().AssemblyQualifiedName,
                                       index + 1, satellitesList.Count);

                });

            satellites.AddRange(satelliteContexts);
        }

        public void Stop()
        {
            Parallel.ForEach(satellites, (context, state, index) =>
                {
                    Logger.DebugFormat("Stopping {1}/{2} {0} satellite", context.Instance.GetType().AssemblyQualifiedName,
                                       index + 1, satellites.Count);

                    if (context.Transport != null)
                    {
                        context.Transport.Stop();
                    }

                    context.Instance.Stop();

                    Logger.InfoFormat("Stopped {1}/{2} {0} satellite", context.Instance.GetType().AssemblyQualifiedName,
                                       index + 1, satellites.Count);
                });
        }

        void StartSatellite(SatelliteContext context)
        {
            Logger.DebugFormat("Starting satellite {0} for {1}.", context.Instance.GetType().AssemblyQualifiedName,
                               context.Instance.InputAddress);

            try
            {
                if (context.Transport != null)
                {
                    //todo: read settings from config
                    var dequeueSettings = new DequeueSettings(context.Instance.InputAddress.Queue,1,false);

                    context.Transport.Start(dequeueSettings);
                }
                else
                {
                    Logger.DebugFormat("No input queue configured for {0}", context.Instance.GetType().AssemblyQualifiedName);
                }

                context.Instance.Start();
            }
            catch (Exception ex)
            {
                Logger.Fatal(string.Format("Satellite {0} failed to start.", context.Instance.GetType().AssemblyQualifiedName), ex);

                throw;
            }
        }

        static ILog Logger = LogManager.GetLogger<SatelliteLauncher>();

        readonly List<SatelliteContext> satellites = new List<SatelliteContext>();
    }
}