namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;


    /// <summary>
    /// </summary>
    public class HardcodedStepRegistration
    {
        string previousStep;
        readonly Action<RegisterStep> addStep;

        internal HardcodedStepRegistration(Action<RegisterStep> addStep, string previousStep)
        {
            this.addStep = addStep;
            this.previousStep = previousStep;
        }

        /// <summary>
        /// Register a new step into the pipeline.
        /// </summary>
        /// <param name="stepId">The identifier of the new step to add.</param>
        /// <param name="behavior">The <see cref="IBehavior{TContext}"/> to execute.</param>
        /// <param name="description">The description of the behavior.</param>
        public HardcodedStepRegistration Register(string stepId, Type behavior, string description)
        {
            BehaviorTypeChecker.ThrowIfInvalid(behavior, "behavior");

            if (string.IsNullOrEmpty(stepId))
            {
                throw new ArgumentNullException("stepId");
            }

            if (string.IsNullOrEmpty(description))
            {
                throw new ArgumentNullException("description");
            }

            var step = RegisterStep.Create(stepId, behavior, description);
            step.InsertAfter(previousStep);
            addStep(step);
            previousStep = stepId;
            return this;
        }


        /// <summary>
        /// <see cref="Register(string,System.Type,string)"/>
        /// </summary>
        /// <param name="wellKnownStep">The identifier of the step to add.</param>
        /// <param name="behavior">The <see cref="IBehavior{TContext}"/> to execute.</param>
        /// <param name="description">The description of the behavior.</param>
        public HardcodedStepRegistration Register(WellKnownStep wellKnownStep, Type behavior, string description)
        {
            if (wellKnownStep == null)
            {
                throw new ArgumentNullException("wellKnownStep");
            }

            Register((string)wellKnownStep, behavior, description);
            return this;
        }
    }

    /// <summary>
    /// Manages the pipeline configuration.
    /// </summary>
    public class PipelineSettings
    {

        /// <summary>
        /// Creates an instance of <see cref="PipelineSettings"/>
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "6")]
        public PipelineSettings(BusConfiguration config)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates an instance of <see cref="PipelineSettings"/>
        /// </summary>
        internal PipelineSettings(PipelineModifications modifications)
        {
            this.modifications = modifications;
        }

        /// <summary>
        /// Removes the specified step from the pipeline.
        /// </summary>
        /// <param name="stepId">The identifier of the step to remove.</param>
        public void Remove(string stepId)
        {
            // I can only remove a behavior that is registered and other behaviors do not depend on, eg InsertBefore/After
            if (string.IsNullOrEmpty(stepId))
            {
                throw new ArgumentNullException("stepId");
            }

            modifications.Removals.Add(new RemoveStep(stepId));
        }

        /// <summary>
        /// Removes the specified step from the pipeline.
        /// </summary>
        /// <param name="wellKnownStep">The identifier of the well known step to remove.</param>
        public void Remove(WellKnownStep wellKnownStep)
        {
            // I can only remove a behavior that is registered and other behaviors do not depend on, eg InsertBefore/After
            if (wellKnownStep == null)
            {
                throw new ArgumentNullException("wellKnownStep");
            }

            Remove((string)wellKnownStep);
        }

        /// <summary>
        /// Replaces an existing step behavior with a new one.
        /// </summary>
        /// <param name="stepId">The identifier of the step to replace its implementation.</param>
        /// <param name="newBehavior">The new <see cref="IBehavior{TContext}"/> to use.</param>
        /// <param name="description">The description of the new behavior.</param>
        public void Replace(string stepId, Type newBehavior, string description = null)
        {
            BehaviorTypeChecker.ThrowIfInvalid(newBehavior, "newBehavior");

            if (string.IsNullOrEmpty(stepId))
            {
                throw new ArgumentNullException("stepId");
            }

            registeredBehaviors.Add(newBehavior);
            modifications.Replacements.Add(new ReplaceBehavior(stepId, newBehavior, description));
        }

        /// <summary>
        /// <see cref="Replace(string,System.Type,string)"/>
        /// </summary>
        /// <param name="wellKnownStep">The identifier of the well known step to replace.</param>
        /// <param name="newBehavior">The new <see cref="IBehavior{TContext}"/> to use.</param>
        /// <param name="description">The description of the new behavior.</param>
        public void Replace(WellKnownStep wellKnownStep, Type newBehavior, string description = null)
        {
            if (wellKnownStep == null)
            {
                throw new ArgumentNullException("wellKnownStep");
            }

            Replace((string)wellKnownStep, newBehavior, description);
        }

        /// <summary>
        /// Register a new step into the pipeline.
        /// </summary>
        /// <param name="stepId">The identifier of the new step to add.</param>
        /// <param name="behavior">The <see cref="IBehavior{TContext}"/> to execute.</param>
        /// <param name="description">The description of the behavior.</param>
        public HardcodedStepRegistration Register(string stepId, Type behavior, string description)
        {
            BehaviorTypeChecker.ThrowIfInvalid(behavior, "behavior");

            if (string.IsNullOrEmpty(stepId))
            {
                throw new ArgumentNullException("stepId");
            }

            if (string.IsNullOrEmpty(description))
            {
                throw new ArgumentNullException("description");
            }

            AddStep(RegisterStep.Create(stepId, behavior, description));
            return new HardcodedStepRegistration(AddStep, stepId);
        }


        /// <summary>
        /// <see cref="Register(string,System.Type,string)"/>
        /// </summary>
        /// <param name="wellKnownStep">The identifier of the step to add.</param>
        /// <param name="behavior">The <see cref="IBehavior{TContext}"/> to execute.</param>
        /// <param name="description">The description of the behavior.</param>
        public HardcodedStepRegistration Register(WellKnownStep wellKnownStep, Type behavior, string description)
        {
            if (wellKnownStep == null)
            {
                throw new ArgumentNullException("wellKnownStep");
            }

            return Register((string)wellKnownStep, behavior, description);
        }


        /// <summary>
        /// Register a new step into the pipeline.
        /// </summary>
        /// <typeparam name="T">The <see cref="RegisterStep"/> to use to perform the registration.</typeparam>
        public void Register<T>() where T : RegisterStep, new()
        {
            AddStep(new T());
        }


        /// <summary>
        /// Register a new step into the pipeline.
        /// </summary>
        /// <param name="registration">The step registration</param>
        public void Register(RegisterStep registration)
        {
            AddStep(registration);
        }
        void AddStep(RegisterStep step)
        {
            registeredSteps.Add(step);

            modifications.Additions.Add(step);
        }

        internal void RegisterBehaviorsInContainer(ReadOnlySettings settings, IConfigureComponents container)
        {
            foreach (var registeredBehavior in registeredBehaviors)
            {
                container.ConfigureComponent(registeredBehavior, DependencyLifecycle.InstancePerCall);
            }

            foreach (var step in registeredSteps)
            {
                step.ApplyContainerRegistration(settings, container);
            }

        }

        List<RegisterStep> registeredSteps = new List<RegisterStep>();
        List<Type> registeredBehaviors = new List<Type>();

        readonly PipelineModifications modifications;

    }
}