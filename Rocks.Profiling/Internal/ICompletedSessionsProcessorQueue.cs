﻿using JetBrains.Annotations;
using Rocks.Profiling.Internal.Implementation;

namespace Rocks.Profiling.Internal
{
    /// <summary>
    ///     A queue for the completed profile session, which will handles the processing.
    /// </summary>
    internal interface ICompletedSessionsProcessorQueue
    {
        /// <summary>
        ///     Add results from completed profiling session.
        /// </summary>
        void Add([NotNull] CompletedSessionInfo completedSessionInfo);
    }
}