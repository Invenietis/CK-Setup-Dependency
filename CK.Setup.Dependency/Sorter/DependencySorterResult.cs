#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setup.Dependency\Sorter\DependencySorterResult.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace CK.Setup;

/// <summary>
/// Encapsulates the result of the <see cref="G:DependencySorter.OrderItems"/> methods.
/// </summary>
public sealed class DependencySorterResult<T> : IDependencySorterResult where T : class, IDependentItem
{
    readonly IReadOnlyList<CycleExplainedElement>? _cycle;
    int _itemIssueWithStructureErrorCount;
    bool _requiredMissingIsError;

    internal DependencySorterResult( List<DependencySorter<T>.Entry>? result,
                                     List<CycleExplainedElement>? cycle,
                                     List<DependentItemIssue> itemIssues,
                                     int startErrorCount,
                                     bool hasStartFatal,
                                     bool hasSevereStructureError )
    {
        Debug.Assert( (result != null) == (cycle == null && !hasStartFatal && !hasSevereStructureError) );
        HasStartFatal = hasStartFatal;
        StartErrorCount = startErrorCount;
        HasSevereStructureError = hasSevereStructureError;
        if( result == null )
        {
            SortedItems = null;
            _cycle = cycle?.ToArray();
        }
        else
        {
            SortedItems = result;
            _cycle = null;
        }
        ItemIssues = itemIssues != null && itemIssues.Count > 0 ? itemIssues : Array.Empty<DependentItemIssue>();
        _requiredMissingIsError = true;
        _itemIssueWithStructureErrorCount = -1;
    }

    /// <summary>
    /// Non null if a cycle has been detected.
    /// </summary>
    public IReadOnlyList<ICycleExplainedElement>? CycleDetected => _cycle;

    /// <summary>
    /// Gets the list of <see cref="ISortedItem{T}"/>: null if <see cref="CycleDetected"/> is not null
    /// or <see cref="HasStartFatal"/> or <see cref="HasSevereStructureError"/> are true.
    /// </summary>
    public readonly IReadOnlyList<ISortedItem<T>>? SortedItems;

    IReadOnlyList<ISortedItem>? IDependencySorterResult.SortedItems => SortedItems;

    /// <summary>
    /// List of <see cref="DependentItemIssue"/>. Never null.
    /// </summary>
    public IReadOnlyList<DependentItemIssue> ItemIssues { get; }

    /// <summary>
    /// Gets the count of <see cref="IDependentItem.StartDependencySort(IActivityMonitor)"/> that signaled an 
    /// error in the monitor.
    /// </summary>
    public int StartErrorCount { get; }

    /// <summary>
    /// Gets whether a <see cref="IDependentItem.StartDependencySort(IActivityMonitor)"/> signaled a
    /// fatal.
    /// </summary>
    public bool HasStartFatal { get; }

    /// <summary>
    /// Gets whether a structure error prevented the graph to be processed.
    /// The <see cref="ItemIssues"/> contains the details of the error.
    /// </summary>
    public bool HasSevereStructureError { get; }

    /// <summary>
    /// Gets or sets whether any non optional missing requirement or generalization is a structure error (<see cref="HasStructureError"/> 
    /// becomes true).
    /// Defaults to true.
    /// </summary>
    public bool ConsiderRequiredMissingAsStructureError
    {
        get { return _requiredMissingIsError; }
        set
        {
            if( _requiredMissingIsError != value )
            {
                _itemIssueWithStructureErrorCount = -1;
                _requiredMissingIsError = value;
            }
        }
    }

    /// <summary>
    /// True if at least one non-optional requirement or generalization (a requirement that is not prefixed with '?' when expressed as a string) exists.
    /// (If both this and <see cref="ConsiderRequiredMissingAsStructureError"/> are true then <see cref="HasStructureError"/> is also true 
    /// since a missing dependency is flagged with <see cref="DependentItemStructureError.MissingDependency"/>.)
    /// </summary>
    public bool HasRequiredMissing
    {
        get
        {
            Debug.Assert( (!ConsiderRequiredMissingAsStructureError || !ItemIssues.Any( m => m.RequiredMissingCount > 0 )) || HasStructureError, "MissingIsError && Exist(Missing) => HasStructureError" );
            return ItemIssues.Any( m => m.RequiredMissingCount > 0 );
        }
    }

    /// <summary>
    /// True if at least one relation between an item and its container is invalid (true when <see cref="HasRequiredMissing"/> is 
    /// true if <see cref="ConsiderRequiredMissingAsStructureError"/> is true).
    /// </summary>
    public bool HasStructureError => StructureErrorCount > 0;

    /// <summary>
    /// Number of items that have at least one invalid relation between itself and its container, its children, its generalization or its dependencies.
    /// </summary>
    public int StructureErrorCount
    {
        get
        {
            if( _itemIssueWithStructureErrorCount < 0 )
            {
                if( _requiredMissingIsError )
                {
                    _itemIssueWithStructureErrorCount = ItemIssues.Count( m => m.StructureError != DependentItemStructureError.None );
                }
                else
                {
                    _itemIssueWithStructureErrorCount = ItemIssues.Count( m => (m.StructureError != DependentItemStructureError.None
                        && m.StructureError != DependentItemStructureError.MissingDependency
                        && m.StructureError != DependentItemStructureError.MissingGeneralization) );
                }
            }
            return _itemIssueWithStructureErrorCount;
        }
    }

    /// <summary>
    /// True only if no cycle has been detected, no structure error (<see cref="HasStructureError"/>) exist.
    /// and no errors have been signaled (<see cref="HasStartFatal"/> must be false and <see cref="StartErrorCount"/> 
    /// must be 0): <see cref="SortedItems"/> can be exploited.
    /// When IsComplete is false, <see cref="LogError"/> can be used to have a dump of the errors in a <see cref="IActivityMonitor"/>.
    /// </summary>
    [MemberNotNullWhen( true, nameof( SortedItems ) )]
    public bool IsComplete => CycleDetected == null && !HasStructureError && !HasStartFatal && StartErrorCount == 0;

    /// <inheritdoc />
    public string? CycleExplainedString => CycleDetected != null ? String.Join( " ", CycleDetected ) : null;

    /// <summary>
    /// Gets a description of the required missing dependencies. 
    /// Null if no missing required dependency exists.
    /// </summary>
    public string? RequiredMissingDependenciesExplained
    {
        get
        {
            string s = String.Join( "', '", ItemIssues.Where( d => d.RequiredMissingCount > 0 )
                                                      .Select( d => "'" + d.Item.FullName + "' => {'" + String.Join( "', '", d.RequiredMissingDependencies ) + "'}" ) );
            return s.Length == 0 ? null : s;
        }
    }

    /// <summary>
    /// Logs <see cref="CycleExplainedString"/> and any structure errors. Does nothing if <see cref="IsComplete"/> is true.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    public void LogError( IActivityMonitor monitor )
    {
        Throw.CheckNotNullArgument( monitor );
        if( HasStructureError )
        {
            foreach( var bug in ItemIssues.Where( d => d.StructureError != DependentItemStructureError.None ) )
            {
                bug.LogError( monitor );
            }
        }
        if( CycleDetected != null )
        {
            monitor.Error( $"Cycle detected: {CycleExplainedString}." );
        }
        if( HasStartFatal )
        {
            monitor.Error( "A fatal error has been raised during sort start." );
        }
        if( StartErrorCount > 0 )
        {
            monitor.Error( $"{StartErrorCount} error(s) have been raised during sort start." );
        }
    }

}
