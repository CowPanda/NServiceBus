namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;


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
        /// <param name="newBehavior">The new <see cref="HomomorphicBehavior{TContext}"/> to use.</param>
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
        /// <param name="newBehavior">The new <see cref="HomomorphicBehavior{TContext}"/> to use.</param>
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
        /// <param name="behavior">The <see cref="HomomorphicBehavior{TContext}"/> to execute.</param>
        /// <param name="description">The description of the behavior.</param>
        /// <param name="isStatic">Is this behavior pipeline-static</param>
        public StepRegistrationSequence Register(string stepId, Type behavior, string description, bool isStatic = false)
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

            AddStep(RegisterStep.Create(stepId, behavior, description, isStatic));
            return new StepRegistrationSequence(AddStep, stepId);
        }

        /// <summary>
        /// Starts a step sequence after a well-defined step.
        /// </summary>
        /// <param name="step"></param>
        /// <returns></returns>
        public StepRegistrationSequence After(WellKnownStep step)
        {
            return new StepRegistrationSequence(AddStep, step);
        }


        /// <summary>
        /// <see cref="Register(string,System.Type,string, bool)"/>
        /// </summary>
        /// <param name="wellKnownStep">The identifier of the step to add.</param>
        /// <param name="behavior">The <see cref="HomomorphicBehavior{TContext}"/> to execute.</param>
        /// <param name="description">The description of the behavior.</param>
        /// <param name="isStatic">Is this behavior pipeline-static</param>
        public StepRegistrationSequence Register(WellKnownStep wellKnownStep, Type behavior, string description, bool isStatic = false)
        {
            if (wellKnownStep == null)
            {
                throw new ArgumentNullException("wellKnownStep");
            }

            return Register((string)wellKnownStep, behavior, description, isStatic);
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
        /// <typeparam name="T">The <see cref="RegisterStep"/> to use to perform the registration.</typeparam>
        /// <typeparam name="TBehavior"></typeparam>
        /// <param name="customInitializer">A function the returns a new instance of the behavior</param>
        public void Register<T,TBehavior>(Func<IBuilder, TBehavior> customInitializer) where T : RegisterStep, new()
        {
            var registration = new T();

            registration.ContainerRegistration((b, s) => customInitializer(b));

            AddStep(registration);
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