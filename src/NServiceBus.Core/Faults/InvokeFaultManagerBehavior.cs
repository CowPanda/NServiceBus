namespace NServiceBus
{
    using System;
    using NServiceBus.Faults;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class InvokeFaultManagerBehavior : IBehavior<IncomingContext>
    {
        public InvokeFaultManagerBehavior(IManageMessageFailures failureManager,CriticalError criticalError)
        {
            this.failureManager = failureManager;
            this.criticalError = criticalError;
        }

        public void Invoke(IncomingContext context, Action next)
        {
            var message = context.PhysicalMessage;
           
            try
            {
                next();
            }
            catch (MessageDeserializationException serializationException)
            {
                Logger.Error("Failed to deserialize message with ID: " + message.Id, serializationException);

                InvokeFaultManager(f => f.SerializationFailedForMessage(message, serializationException),message);
            }

            catch (Exception exception)
            {
                Logger.Error("Failed to process message with ID: " + message.Id, exception);

                InvokeFaultManager(f => f.ProcessingAlwaysFailsForMessage(message, exception),message);
            }
        }

        void InvokeFaultManager(Action<IManageMessageFailures> faultAction,TransportMessage message)
        {
            try
            {
                message.RevertToOriginalBodyIfNeeded();
                faultAction(failureManager);
            }
            catch (Exception ex)
            {
                criticalError.Raise(string.Format("Fault manager failed to process the failed message with id {0}", message.Id), ex);        
                throw;
            }
        }

        readonly IManageMessageFailures failureManager;
        readonly CriticalError criticalError;
        ILog Logger = LogManager.GetLogger<InvokeFaultManagerBehavior>();

        public class Registration : RegisterStep
        {
            public Registration()
                : base("InvokeFaultManager", typeof(InvokeFaultManagerBehavior), "Invokes the configured fault manager for messages that fails processing (and any retries)")
            {
                InsertAfter("ReceiveMessage");

                InsertBeforeIfExists("HandlerTransactionScopeWrapper");
                InsertBeforeIfExists("FirstLevelRetries");
                InsertBeforeIfExists("SecondLevelRetries");

            }
        }
    }
}