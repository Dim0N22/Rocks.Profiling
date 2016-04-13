﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using Rocks.Profiling.Exceptions;
using Rocks.Profiling.Loggers;

namespace Rocks.Profiling.Models
{
    /// <summary>
    ///     Represents a stream of profile events.
    ///     This class is not thread safe.
    /// </summary>
    public sealed class ProfileSession
    {
        #region Private readonly fields

        private readonly Stopwatch stopwatch;
        private readonly IProfilerLogger logger;

        #endregion

        #region Private fields

        [NotNull]
        private ProfileOperation currentOperation;

        #endregion

        #region Construct

        public ProfileSession([NotNull] IProfiler profiler, [NotNull] IProfilerLogger logger)
        {
            if (profiler == null)
                throw new ArgumentNullException(nameof(profiler));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            this.stopwatch = Stopwatch.StartNew();

            this.Profiler = profiler;
            this.logger = logger;

            this.OperationsTreeRoot = new ProfileOperation(this,
                                                           new ProfileOperationSpecification(ProfileOperationNames.ProfileSessionRoot));

            this.currentOperation = this.OperationsTreeRoot;
        }

        #endregion

        #region Public properties

        /// <summary>
        ///     Profiler of the session.
        /// </summary>
        public IProfiler Profiler { get; }

        /// <summary>
        ///     Additional data associated with the session.
        /// </summary>
        public IDictionary<string, object> AdditionalData { get; private set; }

        /// <summary>
        ///     Current time in session.
        /// </summary>
        public TimeSpan Time => this.stopwatch.Elapsed;

        /// <summary>
        ///     Get the total duration of the operations in the session.
        /// </summary>
        public TimeSpan Duration => this.OperationsTreeRoot.Duration;

        /// <summary>
        ///     The root of the session operations tree.
        /// </summary>
        public ProfileOperation OperationsTreeRoot { get; }

        /// <summary>
        ///     Returns true if there is an operation which <see cref="ProfileOperation.Duration" />
        ///     greater or equal to it's <see cref="ProfileOperation.NormalDuration" />.
        /// </summary>
        public bool HasOperationLongerThanNormal { get; private set; }

        #endregion

        #region Public methods

        /// <summary>
        ///     Adds new additional data to the <see cref="AdditionalData" /> dictionary.
        /// </summary>
        public void AddAdditionalData([NotNull] IDictionary<string, object> additionalSessionData)
        {
            if (additionalSessionData == null)
                throw new ArgumentNullException(nameof(additionalSessionData));

            if (this.AdditionalData == null)
                this.AdditionalData = new Dictionary<string, object>(StringComparer.Ordinal);

            foreach (var kv in additionalSessionData)
                this.AdditionalData[kv.Key] = kv.Value;
        }

        #endregion

        #region Protected methods

        /// <summary>
        ///     Starts new operation measure.
        /// </summary>
        [NotNull, MustUseReturnValue]
        internal ProfileOperation StartMeasure([NotNull] ProfileOperationSpecification specification)
        {
            if (specification == null)
                throw new ArgumentNullException(nameof(specification));

            var operation = new ProfileOperation(session: this,
                                                 specification: specification,
                                                 parent: this.currentOperation);

            this.currentOperation.Add(operation);
            this.currentOperation = operation;

            return operation;
        }


        /// <summary>
        ///     Stops operation measure.
        ///     This method should not be called directly - it will be called automatically
        ///     uppon disposing of <see cref="ProfileOperation" /> returned from <see cref="StartMeasure" />.
        /// </summary>
        internal void StopMeasure([NotNull] ProfileOperation operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            try
            {
                if (operation.Session != this)
                    throw new OperationFromAnotherSessionProfilingException();

                if (this.currentOperation != operation)
                    throw new OperationsOutOfOrderProfillingException();

                if (this.currentOperation.Parent == null)
                    throw new OperationsOutOfOrderProfillingException();

                this.OperationsTreeRoot.EndTime = this.currentOperation.EndTime = this.Time;

                if (operation.Duration >= operation.NormalDuration)
                    this.HasOperationLongerThanNormal = true;

                this.currentOperation = this.currentOperation.Parent;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex);
            }
        }

        #endregion
    }
}