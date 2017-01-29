using System;
using System.Collections.Generic;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Non generic interface for <see cref="DependencySorterResult{T}"/>.
    /// </summary>
    public interface IDependencySorterResult
    {
        /// <summary>
        /// Non null if a cycle has been detected.
        /// </summary>
        IReadOnlyList<ICycleExplainedElement> CycleDetected { get; }

        /// <summary>
        /// Gets the list of <see cref="ISortedItem"/>: null if <see cref="CycleDetected"/> is not null.
        /// </summary>
        IReadOnlyList<ISortedItem> SortedItems { get; }

        /// <summary>
        /// Gets or sets whether any non optional missing requirement or generalization is a structure error (<see cref="HasStructureError"/> 
        /// becomes true).
        /// Defaults to true.
        /// </summary>
        bool ConsiderRequiredMissingAsStructureError { get; set; }

        /// <summary>
        /// List of <see cref="DependentItemIssue"/>. Never null.
        /// </summary>
        IReadOnlyList<DependentItemIssue> ItemIssues { get; }

        /// <summary>
        /// True if at least one non-optional requirement or generalization (a requirement that is not prefixed with '?' when expressed as a string) exists.
        /// (If both this and <see cref="ConsiderRequiredMissingAsStructureError"/> are true then <see cref="HasStructureError"/> is also true 
        /// since a missing dependency is flagged with <see cref="DependentItemStructureError.MissingDependency"/>.)
        /// </summary>
        bool HasRequiredMissing { get; }

        /// <summary>
        /// True if at least one relation between an item and its container is invalid (true when <see cref="HasRequiredMissing"/> is 
        /// true if <see cref="ConsiderRequiredMissingAsStructureError"/> is true).
        /// </summary>
        bool HasStructureError { get; }

        /// <summary>
        /// True only if no cycle has been detected, no structure error (<see cref="HasStructureError"/>) exist.
        /// and no errors have been signaled (<see cref="HasStartFatal"/> must be false and <see cref="StartErrorCount"/> 
        /// must be 0): <see cref="SortedItems"/> can be exploited.
        /// When IsComplete is false, <see cref="LogError"/> can be used to have a dump of the errors in a <see cref="IActivityMonitor"/>.
        /// </summary>
        bool IsComplete { get; }

        /// <summary>
        /// Gets the count of <see cref="IDependentItem.StartDependencySort(IActivityMonitor)"/> that signaled an 
        /// error in the monitor.
        /// </summary>
        int StartErrorCount { get; }

        /// <summary>
        /// Gets whether a <see cref="IDependentItem.StartDependencySort(IActivityMonitor)"/> signaled a
        /// fatal.
        /// </summary>
        bool HasStartFatal{ get; }

        /// <summary>
        /// Gets a description of the detected cycle. Null if <see cref="CycleDetected"/> is null.
        /// </summary>
        string CycleExplainedString { get; }

        /// <summary>
        /// Gets a description of the required missing dependencies. 
        /// Null if no missing required dependency exists.
        /// </summary>
        void LogError( IActivityMonitor monitor );

        /// <summary>
        /// Gets a description of the required missing dependencies. 
        /// Null if no missing required dependency exists.
        /// </summary>
        string RequiredMissingDependenciesExplained { get; }

        /// <summary>
        /// Number of items that have at least one invalid relation between itself and its container, its children, its generalization or its dependencies.
        /// </summary>
        int StructureErrorCount { get; }
    }
}
