﻿namespace NServiceBus.Pipeline
{
    using System;
    using System.Linq;
    using NServiceBus.ObjectBuilder;

    abstract class BehaviorInstance
    {
        readonly Type type;
        readonly IBehaviorInvoker invoker;

        protected BehaviorInstance(Type type)
        {
            this.type = type;
            invoker = CreateInvoker(type);
        }

        static IBehaviorInvoker CreateInvoker(Type type)
        {
            var behaviorInterface = type.GetInterfaces().First(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IBehavior<,>));
            var invokerType = typeof(BehaviorInvoker<,>).MakeGenericType(behaviorInterface.GetGenericArguments());
            return (IBehaviorInvoker) Activator.CreateInstance(invokerType);
        }

        public abstract object GetInstance(IBuilder contextBuilder);

        public Type Type { get { return type; } }

        public void Invoke(object behavior, BehaviorContext context, Action<BehaviorContext> next)
        {
            invoker.Invoke(behavior, context, next);
        }
    }
}